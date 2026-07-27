using System;
using UnityEngine;


public class ObservableProperty<T>
{
    private T value;
    public event Action<T, T> OnValueChanged;

    public ObservableProperty(T initialValue)
    {
        value = initialValue;
    }

    public T Value
    {
        get
        {
            return value;
        }

        set
        {
            // 值没有变化，不触发
            if (Equals(this.value, value))
                return;

            T oldValue = this.value;
            this.value = value;
            OnValueChanged?.Invoke(oldValue,this.value);
        }
    }
}
