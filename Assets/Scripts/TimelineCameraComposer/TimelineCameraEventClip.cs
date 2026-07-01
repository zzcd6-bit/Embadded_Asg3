using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public enum TimelineCameraEventParameterType
{
    None,
    Int,
    Float,
    Bool,
    String,
    Vector3,
    GameObject
}

public sealed class TimelineCameraEventClip : PlayableAsset, ITimelineClipAsset
{
    public ExposedReference<GameObject> target;
    public string componentTypeName;
    public string methodName;
    public int parameterCount;
    public string parameter1TypeName;
    public string parameter2TypeName;
    public TimelineCameraEventParameterType parameterType;
    public int intValue;
    public float floatValue;
    public bool boolValue;
    public string stringValue;
    public Vector3 vector3Value;
    public ExposedReference<GameObject> gameObjectValue;
    public int intValue2;
    public float floatValue2;
    public bool boolValue2;
    public string stringValue2;
    public Vector3 vector3Value2;
    public ExposedReference<UnityEngine.Object> objectValue1;
    public ExposedReference<UnityEngine.Object> objectValue2;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<TimelineCameraEventBehaviour>.Create(graph);
    }
}

public sealed class TimelineCameraEventBehaviour : PlayableBehaviour
{
    public static TimelineCameraEventParameterType GetParameterType(Type type)
    {
        return TimelineCameraEventInvocation.GetParameterType(type);
    }
}

public sealed class TimelineCameraEventInvocation
{
    public double TriggerTime;
    public int Order;
    public string DisplayName;
    public GameObject Target;
    public string ComponentTypeName;
    public string MethodName;
    public int ParameterCount;
    public string Parameter1TypeName;
    public string Parameter2TypeName;
    public int IntValue;
    public float FloatValue;
    public bool BoolValue;
    public string StringValue;
    public Vector3 Vector3Value;
    public int IntValue2;
    public float FloatValue2;
    public bool BoolValue2;
    public string StringValue2;
    public Vector3 Vector3Value2;
    public UnityEngine.Object ObjectValue1;
    public UnityEngine.Object ObjectValue2;

    public bool IsValid => Target != null
        && !string.IsNullOrEmpty(ComponentTypeName)
        && !string.IsNullOrEmpty(MethodName);

    public static TimelineCameraEventInvocation FromClip(TimelineClip clip, IExposedPropertyTable resolver, int order = 0)
    {
        TimelineCameraEventClip eventClip = clip?.asset as TimelineCameraEventClip;
        if (eventClip == null)
        {
            return new TimelineCameraEventInvocation { Order = order };
        }

        UnityEngine.Object objectValue1 = Resolve(eventClip.objectValue1, resolver);
        if (objectValue1 == null)
        {
            objectValue1 = Resolve(eventClip.gameObjectValue, resolver);
        }

        return new TimelineCameraEventInvocation
        {
            TriggerTime = Math.Max(0d, clip.start),
            Order = order,
            DisplayName = clip.displayName,
            Target = Resolve(eventClip.target, resolver),
            ComponentTypeName = eventClip.componentTypeName,
            MethodName = eventClip.methodName,
            ParameterCount = eventClip.parameterCount,
            Parameter1TypeName = eventClip.parameter1TypeName,
            Parameter2TypeName = eventClip.parameter2TypeName,
            IntValue = eventClip.intValue,
            FloatValue = eventClip.floatValue,
            BoolValue = eventClip.boolValue,
            StringValue = eventClip.stringValue,
            Vector3Value = eventClip.vector3Value,
            IntValue2 = eventClip.intValue2,
            FloatValue2 = eventClip.floatValue2,
            BoolValue2 = eventClip.boolValue2,
            StringValue2 = eventClip.stringValue2,
            Vector3Value2 = eventClip.vector3Value2,
            ObjectValue1 = objectValue1,
            ObjectValue2 = Resolve(eventClip.objectValue2, resolver)
        };
    }

    public void InvokeTargetMethod()
    {
        if (Target == null || string.IsNullOrEmpty(ComponentTypeName) || string.IsNullOrEmpty(MethodName))
        {
            return;
        }

        Component component = FindComponent(Target, ComponentTypeName);
        if (component == null)
        {
            Debug.LogWarning($"Timeline event '{EventLabel}' could not find component {ComponentTypeName} on {Target.name}.", Target);
            return;
        }

        MethodInfo method = FindMethod(component.GetType(), MethodName, ParameterCount, Parameter1TypeName, Parameter2TypeName);
        if (method == null)
        {
            Debug.LogWarning($"Timeline event '{EventLabel}' could not find method {MethodName} on {component.GetType().Name}.", component);
            return;
        }

        try
        {
            method.Invoke(component, GetParameterValues(method));
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            Debug.LogException(exception.InnerException, component);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, component);
        }
    }

    public static TimelineCameraEventParameterType GetParameterType(Type type)
    {
        if (type == null)
        {
            return TimelineCameraEventParameterType.None;
        }

        if (type == typeof(int))
        {
            return TimelineCameraEventParameterType.Int;
        }

        if (type == typeof(float))
        {
            return TimelineCameraEventParameterType.Float;
        }

        if (type == typeof(bool))
        {
            return TimelineCameraEventParameterType.Bool;
        }

        if (type == typeof(string))
        {
            return TimelineCameraEventParameterType.String;
        }

        if (type == typeof(Vector3))
        {
            return TimelineCameraEventParameterType.Vector3;
        }

        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
        {
            return TimelineCameraEventParameterType.GameObject;
        }

        return TimelineCameraEventParameterType.None;
    }

    private string EventLabel => string.IsNullOrWhiteSpace(DisplayName) ? MethodName : DisplayName;

    private object[] GetParameterValues(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return null;
        }

        object[] values = new object[parameters.Length];
        values[0] = GetParameterValue(parameters[0].ParameterType, false);
        if (parameters.Length > 1)
        {
            values[1] = GetParameterValue(parameters[1].ParameterType, true);
        }

        return values;
    }

    private object GetParameterValue(Type expectedType, bool second)
    {
        TimelineCameraEventParameterType parameterType = GetParameterType(expectedType);
        switch (parameterType)
        {
            case TimelineCameraEventParameterType.Int:
                return second ? IntValue2 : IntValue;
            case TimelineCameraEventParameterType.Float:
                return second ? FloatValue2 : FloatValue;
            case TimelineCameraEventParameterType.Bool:
                return second ? BoolValue2 : BoolValue;
            case TimelineCameraEventParameterType.String:
                return second ? StringValue2 : StringValue;
            case TimelineCameraEventParameterType.Vector3:
                return second ? Vector3Value2 : Vector3Value;
            case TimelineCameraEventParameterType.GameObject:
                return GetObjectParameterValue(expectedType, second ? ObjectValue2 : ObjectValue1);
            default:
                return null;
        }
    }

    private static object GetObjectParameterValue(Type expectedType, UnityEngine.Object value)
    {
        if (value == null || expectedType == null)
        {
            return value;
        }

        if (expectedType.IsInstanceOfType(value))
        {
            return value;
        }

        if (expectedType == typeof(GameObject) && value is Component component)
        {
            return component.gameObject;
        }

        if (typeof(Component).IsAssignableFrom(expectedType) && value is GameObject gameObject)
        {
            return gameObject.GetComponent(expectedType);
        }

        return value;
    }

    private static Component FindComponent(GameObject target, string componentTypeName)
    {
        Component[] components = target.GetComponents<Component>();
        Component component = FindComponentInList(components, componentTypeName);
        if (component != null)
        {
            return component;
        }

        components = target.GetComponentsInChildren<Component>(true);
        component = FindComponentInList(components, componentTypeName);
        if (component != null)
        {
            return component;
        }

        components = target.GetComponentsInParent<Component>(true);
        return FindComponentInList(components, componentTypeName);
    }

    private static Component FindComponentInList(Component[] components, string componentTypeName)
    {
        foreach (Component component in components)
        {
            if (component == null)
            {
                continue;
            }

            Type type = component.GetType();
            if (type.AssemblyQualifiedName == componentTypeName || type.FullName == componentTypeName)
            {
                return component;
            }
        }

        return null;
    }

    private static MethodInfo FindMethod(Type componentType, string methodName, int parameterCount, string parameter1TypeName, string parameter2TypeName)
    {
        MethodInfo[] methods = componentType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach (MethodInfo method in methods)
        {
            if (method.Name != methodName || method.IsSpecialName)
            {
                continue;
            }

            if (DoesSignatureMatch(method, parameterCount, parameter1TypeName, parameter2TypeName))
            {
                return method;
            }
        }

        return null;
    }

    private static bool DoesSignatureMatch(MethodInfo method, int parameterCount, string parameter1TypeName, string parameter2TypeName)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length != parameterCount)
        {
            return false;
        }

        if (parameterCount > 0 && !DoesParameterTypeMatch(parameters[0].ParameterType, parameter1TypeName))
        {
            return false;
        }

        if (parameterCount > 1 && !DoesParameterTypeMatch(parameters[1].ParameterType, parameter2TypeName))
        {
            return false;
        }

        return true;
    }

    private static bool DoesParameterTypeMatch(Type parameterType, string typeName)
    {
        return parameterType.AssemblyQualifiedName == typeName || parameterType.FullName == typeName;
    }

    private static T Resolve<T>(ExposedReference<T> reference, IExposedPropertyTable resolver)
        where T : UnityEngine.Object
    {
        return resolver == null ? (T)reference.defaultValue : reference.Resolve(resolver);
    }
}
