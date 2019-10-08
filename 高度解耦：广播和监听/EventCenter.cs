using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventCenter
{
    private static Dictionary<CBEventType, Delegate> m_EventTable = new Dictionary<CBEventType, Delegate>();

    private static void OnListenerAdding(CBEventType CBEventType, Delegate callBack)
    {
        if (!m_EventTable.ContainsKey(CBEventType))
        {
            m_EventTable.Add(CBEventType, null);
        }
        Delegate d = m_EventTable[CBEventType];
        if (d != null && d.GetType() != callBack.GetType())
        {
            throw new Exception(string.Format("尝试为事件{0}添加不同类型的委托，当前事件所对应的委托是{1}，要添加的委托类型为{2}", CBEventType, d.GetType(), callBack.GetType()));
        }
    }
    private static void OnListenerRemoving(CBEventType CBEventType, Delegate callBack)
    {
        if (m_EventTable.ContainsKey(CBEventType))
        {
            Delegate d = m_EventTable[CBEventType];
            if (d == null)
            {
                throw new Exception(string.Format("移除监听错误：事件{0}没有对应的委托", CBEventType));
            }
            else if (d.GetType() != callBack.GetType())
            {
                throw new Exception(string.Format("移除监听错误：尝试为事件{0}移除不同类型的委托，当前委托类型为{1}，要移除的委托类型为{2}", CBEventType, d.GetType(), callBack.GetType()));
            }
        }
        else
        {
            throw new Exception(string.Format("移除监听错误：没有事件码{0}", CBEventType));
        }
    }
    private static void OnListenerRemoved(CBEventType CBEventType)
    {
        if (m_EventTable[CBEventType] == null)
        {
            m_EventTable.Remove(CBEventType);
        }
    }
    //no parameters
    public static void AddListener(CBEventType CBEventType, CallBack callBack)
    {
        OnListenerAdding(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack)m_EventTable[CBEventType] + callBack;
    }
    //Single parameters
    public static void AddListener<T>(CBEventType CBEventType, CallBack<T> callBack)
    {
        OnListenerAdding(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack<T>)m_EventTable[CBEventType] + callBack;
    }
    //two parameters
    public static void AddListener<T, X>(CBEventType CBEventType, CallBack<T, X> callBack)
    {
        OnListenerAdding(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack<T, X>)m_EventTable[CBEventType] + callBack;
    }
    //three parameters
    public static void AddListener<T, X, Y>(CBEventType CBEventType, CallBack<T, X, Y> callBack)
    {
        OnListenerAdding(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack<T, X, Y>)m_EventTable[CBEventType] + callBack;
    }
    //four parameters
    public static void AddListener<T, X, Y, Z>(CBEventType CBEventType, CallBack<T, X, Y, Z> callBack)
    {
        OnListenerAdding(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack<T, X, Y, Z>)m_EventTable[CBEventType] + callBack;
    }
    //five parameters
    public static void AddListener<T, X, Y, Z, W>(CBEventType CBEventType, CallBack<T, X, Y, Z, W> callBack)
    {
        OnListenerAdding(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack<T, X, Y, Z, W>)m_EventTable[CBEventType] + callBack;
    }

    //no parameters
    public static void RemoveListener(CBEventType CBEventType, CallBack callBack)
    {
        OnListenerRemoving(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack)m_EventTable[CBEventType] - callBack;
        OnListenerRemoved(CBEventType);
    }
    //single parameters
    public static void RemoveListener<T>(CBEventType CBEventType, CallBack<T> callBack)
    {
        OnListenerRemoving(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack<T>)m_EventTable[CBEventType] - callBack;
        OnListenerRemoved(CBEventType);
    }
    //two parameters
    public static void RemoveListener<T, X>(CBEventType CBEventType, CallBack<T, X> callBack)
    {
        OnListenerRemoving(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack<T, X>)m_EventTable[CBEventType] - callBack;
        OnListenerRemoved(CBEventType);
    }
    //three parameters
    public static void RemoveListener<T, X, Y>(CBEventType CBEventType, CallBack<T, X, Y> callBack)
    {
        OnListenerRemoving(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack<T, X, Y>)m_EventTable[CBEventType] - callBack;
        OnListenerRemoved(CBEventType);
    }
    //four parameters
    public static void RemoveListener<T, X, Y, Z>(CBEventType CBEventType, CallBack<T, X, Y, Z> callBack)
    {
        OnListenerRemoving(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack<T, X, Y, Z>)m_EventTable[CBEventType] - callBack;
        OnListenerRemoved(CBEventType);
    }
    //five parameters
    public static void RemoveListener<T, X, Y, Z, W>(CBEventType CBEventType, CallBack<T, X, Y, Z, W> callBack)
    {
        OnListenerRemoving(CBEventType, callBack);
        m_EventTable[CBEventType] = (CallBack<T, X, Y, Z, W>)m_EventTable[CBEventType] - callBack;
        OnListenerRemoved(CBEventType);
    }


    //no parameters
    public static void Broadcast(CBEventType CBEventType)
    {
        Delegate d;
        if (m_EventTable.TryGetValue(CBEventType, out d))
        {
            CallBack callBack = d as CallBack;
            if (callBack != null)
            {
                callBack();
            }
            else
            {
                throw new Exception(string.Format("广播事件错误：事件{0}对应委托具有不同的类型", CBEventType));
            }
        }
    }
    //single parameters
    public static void Broadcast<T>(CBEventType CBEventType, T arg)
    {
        Delegate d;
        if (m_EventTable.TryGetValue(CBEventType, out d))
        {
            CallBack<T> callBack = d as CallBack<T>;
            if (callBack != null)
            {
                callBack(arg);
            }
            else
            {
                throw new Exception(string.Format("广播事件错误：事件{0}对应委托具有不同的类型", CBEventType));
            }
        }
    }
    //two parameters
    public static void Broadcast<T, X>(CBEventType CBEventType, T arg1, X arg2)
    {
        Delegate d;
        if (m_EventTable.TryGetValue(CBEventType, out d))
        {
            CallBack<T, X> callBack = d as CallBack<T, X>;
            if (callBack != null)
            {
                callBack(arg1, arg2);
            }
            else
            {
                throw new Exception(string.Format("广播事件错误：事件{0}对应委托具有不同的类型", CBEventType));
            }
        }
    }
    //three parameters
    public static void Broadcast<T, X, Y>(CBEventType CBEventType, T arg1, X arg2, Y arg3)
    {
        Delegate d;
        if (m_EventTable.TryGetValue(CBEventType, out d))
        {
            CallBack<T, X, Y> callBack = d as CallBack<T, X, Y>;
            if (callBack != null)
            {
                callBack(arg1, arg2, arg3);
            }
            else
            {
                throw new Exception(string.Format("广播事件错误：事件{0}对应委托具有不同的类型", CBEventType));
            }
        }
    }
    //four parameters
    public static void Broadcast<T, X, Y, Z>(CBEventType CBEventType, T arg1, X arg2, Y arg3, Z arg4)
    {
        Delegate d;
        if (m_EventTable.TryGetValue(CBEventType, out d))
        {
            CallBack<T, X, Y, Z> callBack = d as CallBack<T, X, Y, Z>;
            if (callBack != null)
            {
                callBack(arg1, arg2, arg3, arg4);
            }
            else
            {
                throw new Exception(string.Format("广播事件错误：事件{0}对应委托具有不同的类型", CBEventType));
            }
        }
    }
    //five parameters
    public static void Broadcast<T, X, Y, Z, W>(CBEventType CBEventType, T arg1, X arg2, Y arg3, Z arg4, W arg5)
    {
        Delegate d;
        if (m_EventTable.TryGetValue(CBEventType, out d))
        {
            CallBack<T, X, Y, Z, W> callBack = d as CallBack<T, X, Y, Z, W>;
            if (callBack != null)
            {
                callBack(arg1, arg2, arg3, arg4, arg5);
            }
            else
            {
                throw new Exception(string.Format("广播事件错误：事件{0}对应委托具有不同的类型", CBEventType));
            }
        }
    }
}
