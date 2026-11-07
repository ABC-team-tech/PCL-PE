namespace MeloongCore;
public static class GeneralUtils {

    public static bool IsGenericInstanceOf(object? instance, Type genericTypeDefinition) {
        if (instance is null) return false;
        for (var type = instance.GetType(); type is not null; type = type.BaseType) {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition) return true;
        }
        return false;
    }

}
