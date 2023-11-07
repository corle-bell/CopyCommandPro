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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace UnityEditor
{
#if UNITY_EDITOR
    [InitializeOnLoadAttribute]
    public static class CopyCommandHelper
    {
        //��ʼ����ʱ,ע���¼�������
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

            if (CopyCommandHelper.FieldInfoTarget.GetType()==component.GetType())
            {
                EditorUtility.CopySerialized(CopyCommandHelper.FieldInfoTarget, component);
            }
            else
            {
                SetFieldInfos(CopyCommandHelper.FieldInfos, component, CopyCommandHelper.FieldInfoTarget);
            }
            
            CopyCommandHelper.FieldInfoTarget = null;
            CopyCommandHelper.FieldInfos.Clear();
            CopyCommandHelper.FieldInfos = null;
            
            EditorUtility.SetDirty(component);
        }

        private static object CreateInstance(object src)
        {
            Type type = src.GetType();
            object retval = null;

            try
            {
                ConstructorInfo[] constructors = type.GetConstructors();

                for (int i = 0; i < constructors.Length; i++)
                {
                    var p = constructors[i].GetParameters();
                    if (p==null || p.Length==0)
                    {
                        retval = Activator.CreateInstance(type);
                        return retval;
                    }
                }

                // �ֶ����ù��캯�������ݲ���
                ConstructorInfo constructor = constructors[0];
                ParameterInfo[] paramsInfos = constructor.GetParameters();
                object[] parameters = new object [paramsInfos.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    parameters[i] = paramsInfos[i].ParameterType.IsClass
                        ? null
                        : Activator.CreateInstance(paramsInfos[i].ParameterType);
                }

                if (constructor != null)
                {
                    retval = constructor.Invoke(parameters);
                }
                else
                {
                    // �����Ҳ������ʹ��캯�������  
                }
            }
            catch (Exception e)
            {
                
            }
            
            return retval;
        }
        
        private static object DeepCopy(object obj)
        {
            var objType = obj.GetType();
            //������ַ�����ֵ������ֱ�ӷ���
            if (obj is string || objType.IsValueType) return obj;
            
            object retval = CreateInstance(obj);
            
            FieldInfo[] fields = objType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                try
                {
                    EditorUtility.CopySerializedManagedFieldsOnly(field.GetValue(obj), retval);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }
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
                
                if (type.IsClass && !isSerializable(type))
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        ret.SetValue(arr.GetValue(i), i);
                    }
                }
                else
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        ret.SetValue(DeepCopy(arr.GetValue(i)), i);
                    }
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
                        //����classΪSerializable ��Ҫ�������
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
            
            //����������� ��ô������ϼ��Ļ�������ȡ�ǹ��б�������Ϣ ��ͨ��SerializeFieldɸѡ
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
