//*************************************************
//----Author:       Cyy 
//
//----CreateDate:   2022-10-27 14:06:53
//
//----Desc:         Create By BM
//
//**************************************************

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor
{
#if UNITY_EDITOR
    [InitializeOnLoadAttribute]
    public static class CopyCommandHelper
    {
        //初始化类时,注册事件处理函数
        static CopyCommandHelper()
        {
            
        }

        public static Component FieldInfoTarget;
        public static List<FieldInfo> FieldInfos;
    }
#endif
    
    public class CopyCommandPro : EditorWindow
    {
        //SerializedProperty

        [MenuItem("CONTEXT/Component/Copy Component Pro")]
        static void CopyComponentPro(MenuCommand cmd)
        {
            Component component = cmd.context as Component;
            if (component == null) return;
            CopyCommandHelper.FieldInfos = GetFields(component);
            CopyCommandHelper.FieldInfoTarget = component;
        }
        
        [MenuItem("CONTEXT/Component/Paste Component Pro")]
        static void PasteComponentPro(MenuCommand cmd)
        {
            Component component = cmd.context as Component;
            if (component == null
                || CopyCommandHelper.FieldInfoTarget==null
                || CopyCommandHelper.FieldInfos==null) return;
            if(CopyCommandHelper.FieldInfoTarget==component)return;
            
            SetFieldInfos(CopyCommandHelper.FieldInfos, component, CopyCommandHelper.FieldInfoTarget);
            CopyCommandHelper.FieldInfoTarget = null;
            CopyCommandHelper.FieldInfos = null;
            
            EditorUtility.SetDirty(component);
        }
        
        private static object DeepCopy(object obj)
        {
            //如果是字符串或值类型则直接返回
            if (obj is string || obj.GetType().IsValueType) return obj;
            
            object retval = Activator.CreateInstance(obj.GetType());
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                try { field.SetValue(retval, DeepCopy(field.GetValue(obj))); }
                catch { }
            }
            return retval;
        }

        private static object DeepCopyWithCheck(object obj)
        {
            if (obj.GetType().IsArray)
            {
                
                var arr = obj as Array;
                if (arr.Length == 0) return null;
                
                var type = arr.GetValue(0).GetType();
                Array ret = Array.CreateInstance(type, arr.Length);
                
                for (int i = 0; i < arr.Length; i++)
                {
                    ret.SetValue(DeepCopy(arr.GetValue(i)), i);
                }
                
                return ret;
            }
            else
            {
                return DeepCopy(obj);
            }
        }

        private static bool isSerializable(Type type)
        {
            Func<System.Attribute[], bool> IsAtt1 = o =>
            {
                foreach (System.Attribute a in o)
                {
                    if (a is System.SerializableAttribute)
                        return true;
                }
                return false;
            };
            return IsAtt1(System.Attribute.GetCustomAttributes(type, true));
        }

        private static void SetFieldInfos(List<FieldInfo> datas, Component _dest, Component _src)
        {
            if(datas==null || datas.Count<=0)return;
            
            Undo.RecordObject(_dest, "Copy Component Pro Paste");
            
            foreach (var item in datas)
            {
                FieldInfo fieldInfo = _dest.GetType().GetField(item.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo!=null && fieldInfo.FieldType==item.FieldType)
                {
                    object t = item.GetValue(_src);
                    if (item.FieldType.IsClass)
                    {
                        //这里class为Serializable 需要进行深拷贝
                        if (isSerializable(item.FieldType))
                        {
                            t = DeepCopyWithCheck(t);
                        }
                    }
                    
                    fieldInfo.SetValue(_dest, t);
                    
                    
                }
            }
            
        }

        private static List<FieldInfo> GetFields(Component t)
        {
            List<FieldInfo> ListStr = new List<FieldInfo>();
            if (t == null)
            {
                return ListStr;
            }
            
            
            var _type = t.GetType();
            List<FieldInfo> fields = new List<FieldInfo>();
            FieldInfo[] public_fields = _type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                                                   | BindingFlags.Static);
            if (public_fields != null && public_fields.Length>0)
            {
                fields.AddRange(public_fields);
            }
            
            //如果是派生类 那么获得最上级的基类来获取非公有变量的信息 在通过SerializeField筛选
            while (_type.BaseType!=null && _type.BaseType!=typeof(MonoBehaviour))
            {
                _type = _type.BaseType;
                
                FieldInfo[] non_fields = _type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic
                    | BindingFlags.Static);
                
                if (non_fields != null && non_fields.Length>0)
                {
                    fields.AddRange(non_fields);
                }
            }
            
            if (fields.Count <= 0)
            {
                return ListStr;
            }
            
            foreach (FieldInfo item in fields)
            {
                if (item.IsPublic)
                {
                    ListStr.Add(item);
                }
                else
                {
                    object[] Attribute1 = item.GetCustomAttributes(true);
                    bool isSerializeField=false;
                    foreach(var o in Attribute1)
                    {
                        if (o.ToString().Contains("SerializeField"))
                        {
                            isSerializeField = true;
                        }
                    }

                    if (isSerializeField)
                    {
                        ListStr.Add(item);
                    }
                }
            }
            return ListStr;
        }
    }
}
