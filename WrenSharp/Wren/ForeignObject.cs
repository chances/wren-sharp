using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Wren.Attributes;

namespace Wren
{
    public abstract class ForeignObject : IDisposable
    {
        internal VirtualMachine VirtualMachine { get; set; }

        [WrenIgnore]
        public virtual void Dispose() {}

        protected void AbortFiber(string error)
        {
            VirtualMachine.SetSlotString(0, error);
            VirtualMachine.AbortFiber(0);
        }

        internal static ForeignObject Allocate<T>(VirtualMachine vm) where T : ForeignObject
        {
            // Try to allocate given this object's constructors with descending arity
            var constructors = typeof(T).GetConstructors().Where(IsPublic)
                .OrderByDescending(ctor => ctor.GetParameters().Length).ToList();
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters().Reverse().ToList();
                var givenParameters = new List<object>();
                var parametersMatch = true;
                if (parameters.Count > 0)
                {
                    for (int i = parameters.Count; i > 0; i--)
                    {
                        var param = parameters[i - 1];
                        switch (vm.GetSlotType(i))
                        {
                            case Wren.ValueType.WREN_TYPE_BOOL:
                                parametersMatch &= param.ParameterType == typeof(bool);
                                if (parametersMatch) givenParameters.Insert(0, vm.GetSlotBool(i));
                                break;
                            case Wren.ValueType.WREN_TYPE_NUM:
                                parametersMatch &= param.ParameterType == typeof(double);
                                if (parametersMatch) givenParameters.Insert(0, vm.GetSlotDouble(i));
                                break;
                            case Wren.ValueType.WREN_TYPE_STRING:
                                parametersMatch &= param.ParameterType == typeof(string);
                                if (parametersMatch) givenParameters.Insert(0, vm.GetSlotString(i));
                                break;
                            default:
                                parametersMatch = false;
                                break;
                        }
                    }
                }

                if (parametersMatch)
                {
                    var foreignObject = (ForeignObject) ctor.Invoke(givenParameters.ToArray());
                    foreignObject.VirtualMachine = vm;
                    return foreignObject;
                }
            }

            return null;
        }

        internal static Dictionary<(bool, string), ForeignMethodFn> Methods<T>() where T : ForeignObject
        {
            var methods = typeof(T).GetMethods().Where(IsPublic)
                .Where(HasCompatibleReturn).ToList();
            return methods.ToDictionary(method =>
            {
                var methodName = $"{method.Name[0].ToString().ToLowerInvariant()}{method.Name.Substring(1)}";
                var paramSignature = string.Join(",", method.GetParameters().Select(_ => "_"));
                return (method.IsStatic, $"{methodName}({paramSignature})");
            }, method =>
            {
                var methodName = method.Name;
                var parameters = method.GetParameters();
                Func<object, object[], object> invoke = method.Invoke;
                var returnType = method.ReturnType;
                ForeignMethodFn foreignMethod = (vm) =>
                {
                    var foreignObject = vm.GetSlotForeign(0);
                    if (foreignObject == null || !(foreignObject is ForeignObject)) return;
                    var actualParameters = new List<object>();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var paramSlot = i + 1;
                        var parameterName = parameters[i].Name;
                        var parameterType = parameters[i].ParameterType;
                        var actualParamType = vm.GetSlotType(paramSlot);
                        (bool paramTypeMatches, object actualParam) =
                            GetActualParameter(parameterType, actualParamType, paramSlot, vm);
                        if (paramTypeMatches) actualParameters.Add(actualParam);
                        else
                        {
                            var formalParamType = actualParam.GetType();
                            ((ForeignObject) foreignObject).AbortFiber(
                                $"Foreign method '{methodName}' parameter '{parameterName}' type " +
                                    $"mismatch given actual parameter of type {formalParamType} " +
                                    $"({actualParam} in slot {paramSlot}), expected type " +
                                    $"{parameterType.Name}"
                            );
                            return;
                        }
                    }

                    var returnValue = invoke(foreignObject, actualParameters.ToArray());
                    if (returnType != typeof(void))
                    {
                        vm.SetSlot(0, returnValue);
                    }
                };
                return foreignMethod;
            });
        }

        #region Reflection Helpers
        private static bool IsPublic(MethodBase method) =>
            method.IsPublic && HasCompatibleParameters(method) && IsNotIgnored(method);
        private static bool HasCompatibleParameters(MethodBase method)
        {
            if (method.ContainsGenericParameters) return false;
            var parameters = method.GetParameters();
            if (parameters.Length == 0) return true;
            return parameters.All(param =>
                param.ParameterType == typeof(bool) ||
                param.ParameterType == typeof(int) ||
                param.ParameterType == typeof(double) ||
                param.ParameterType == typeof(string) ||
                param.ParameterType == typeof(object)
            );
        }
        private static bool HasCompatibleReturn(MethodInfo method)
        {
            return method.ReturnType == typeof(bool) ||
                method.ReturnType == typeof(int) ||
                method.ReturnType == typeof(double) ||
                method.ReturnType == typeof(string) ||
                method.ReturnType == typeof(void);
        }
        private static bool IsNotIgnored(MemberInfo member) =>
            member.GetCustomAttribute<WrenIgnoreAttribute>() == null;
        private static (bool, object) GetActualParameter(Type formalType, ValueType actual, int slot, VirtualMachine vm)
        {
            switch (actual)
            {
                case ValueType.WREN_TYPE_BOOL:
                    return (formalType == typeof(bool) || formalType == typeof(object), vm.GetSlotBool(slot));
                case ValueType.WREN_TYPE_NUM:
                    object formalParameter;

                    if (formalType == typeof(int))
                    {
                        formalParameter = (int) vm.GetSlotDouble(slot);
                    }
                    else if (formalType == typeof(double) || formalType == typeof(object))
                    {
                        formalParameter = vm.GetSlotDouble(slot);
                    }
                    else
                    {
                        return (false, vm.GetSlotDouble(slot));
                    }

                    return (true, formalParameter);
                case ValueType.WREN_TYPE_STRING:
                    return (formalType == typeof(string) || formalType == typeof(object), vm.GetSlotString(slot));
                default:
                    return (false, null);
            }
        }
        #endregion
    }
}
