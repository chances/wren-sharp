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
                    var givenParameters = new List<object>();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var paramSlot = i + 1;
                        var parameterType = parameters[i].ParameterType;
                        var givenParamType = vm.GetSlotType(paramSlot);
                        (bool paramTypeMatches, object givenParam) =
                            GivenParamTypeMatches(parameterType, givenParamType, paramSlot, vm);
                        if (paramTypeMatches) givenParameters.Add(givenParam);
                        else
                        {
                            ((ForeignObject) foreignObject).AbortFiber(
                                $"Foreign method '{methodName}' type mismatch given formal parameter {paramSlot}, expected type {parameterType.Name}");
                            return;
                        }
                    }

                    var returnValue = invoke(foreignObject, givenParameters.ToArray());
                    if (returnType != typeof(void))
                    {
                        vm.SetSlot(0, returnValue);
                    }
                };
                return foreignMethod;
            });
        }

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
        private static (bool, object) GivenParamTypeMatches(Type paramType, ValueType given, int slot, VirtualMachine vm)
        {
            switch (given)
            {
                case ValueType.WREN_TYPE_BOOL:
                    return (paramType == typeof(bool) || paramType == typeof(object), vm.GetSlotBool(slot));
                case ValueType.WREN_TYPE_NUM:
                    // TODO: Explicitly cast formal given double params to int where paramType is int
                    return (paramType == typeof(int) || paramType == typeof(double) || paramType == typeof(object), vm.GetSlotDouble(slot));
                case ValueType.WREN_TYPE_STRING:
                    return (paramType == typeof(string) || paramType == typeof(object), vm.GetSlotString(slot));
                default:
                    return (false, null);
            }
        }
    }
}
