using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Vexe.Fast.Serializer.Internal
{
    public class ILEmitter
    {
        public ILGenerator il;

        public ILEmitter ret_s(int value) { return ldc_i4_s(value).ret(); }
        public ILEmitter ret(int value) { return ldc_i4(value).ret(); }
        public ILEmitter ret() { il.Emit(OpCodes.Ret); return this; }
        public ILEmitter retnull() { return ldnull().ret(); }
        public ILEmitter ret(LocalBuilder local) { return ldloc(local).ret(); }
        public ILEmitter ret_s(LocalBuilder local) { return ldloc_s(local).ret(); }
        public ILEmitter cast(Type type) { il.Emit(OpCodes.Castclass, type); return this; }
        public ILEmitter cast<T>() { return cast(typeof(T)); }
        public ILEmitter box(Type type) { il.Emit(OpCodes.Box, type); return this; }
        public ILEmitter box<T>() { il.Emit(OpCodes.Box, typeof(T)); return this; }
        public ILEmitter unbox_any(Type type) { il.Emit(OpCodes.Unbox_Any, type); return this; }
        public ILEmitter unbox(Type type) { il.Emit(OpCodes.Unbox, type); return this; }
        public ILEmitter call(MethodInfo method) { il.Emit(OpCodes.Call, method); return this; }
        public ILEmitter callvirt(MethodInfo method) { il.Emit(OpCodes.Callvirt, method); return this; }
        public ILEmitter ldnull() { il.Emit(OpCodes.Ldnull); return this; }
        public ILEmitter bne_un(Label target) { il.Emit(OpCodes.Bne_Un, target); return this; }
        public ILEmitter beq(Label target) { il.Emit(OpCodes.Beq, target); return this; }
        public ILEmitter ldc_i4_0() { il.Emit(OpCodes.Ldc_I4_0); return this; }
        public ILEmitter ldc_i4_1() { il.Emit(OpCodes.Ldc_I4_1); return this; }
        public ILEmitter ldc_i4_2() { il.Emit(OpCodes.Ldc_I4_2); return this; }
        public ILEmitter ldc_i4(int c) { il.Emit(OpCodes.Ldc_I4, c); return this; }
        public ILEmitter starg_0() { il.Emit(OpCodes.Starg, 0); return this; }
        public ILEmitter starg_1() { il.Emit(OpCodes.Starg, 1); return this; }
        public ILEmitter starg_2() { il.Emit(OpCodes.Starg, 2); return this; }
        public ILEmitter starg_s(int n) { il.Emit(OpCodes.Starg_S, n); return this; }
        public ILEmitter ldarg_0() { il.Emit(OpCodes.Ldarg_0); return this; }
        public ILEmitter ldarg_1() { il.Emit(OpCodes.Ldarg_1); return this; }
        public ILEmitter ldarg_2() { il.Emit(OpCodes.Ldarg_2); return this; }
        public ILEmitter ldarg_3() { il.Emit(OpCodes.Ldarg_3); return this; }
        public ILEmitter ldarga(int idx) { il.Emit(OpCodes.Ldarga, idx); return this; }
        public ILEmitter ldarga_s(int idx) { il.Emit(OpCodes.Ldarga_S, idx); return this; }
        public ILEmitter ldarg(int idx) { il.Emit(OpCodes.Ldarg, idx); return this; }
        public ILEmitter ldarg_s(int idx) { il.Emit(OpCodes.Ldarg_S, idx); return this; }
        public ILEmitter ifclass_ldind_ref(Type type) { if (!type.IsValueType) il.Emit(OpCodes.Ldind_Ref); return this; }
        public ILEmitter ldloc_0() { il.Emit(OpCodes.Ldloc_0); return this; }
        public ILEmitter ldloc_1() { il.Emit(OpCodes.Ldloc_1); return this; }
        public ILEmitter ldloc_2() { il.Emit(OpCodes.Ldloc_2); return this; }
        public ILEmitter ldloca_s(int idx) { il.Emit(OpCodes.Ldloca_S, idx); return this; }
        public ILEmitter ldloca_s(LocalBuilder local) { il.Emit(OpCodes.Ldloca_S, local); return this; }
        public ILEmitter ldloc_s(int idx) { il.Emit(OpCodes.Ldloc_S, idx); return this; }
        public ILEmitter ldloc_s(LocalBuilder local) { il.Emit(OpCodes.Ldloc_S, local); return this; }
        public ILEmitter ldloca(int idx) { il.Emit(OpCodes.Ldloca, idx); return this; }
        public ILEmitter ldloca(LocalBuilder local) { il.Emit(OpCodes.Ldloca, local); return this; }
        public ILEmitter ldloc(int idx) { il.Emit(OpCodes.Ldloc, idx); return this; }
        public ILEmitter ldloc(LocalBuilder local) { il.Emit(OpCodes.Ldloc, local); return this; }
        public ILEmitter initobj(Type type) { il.Emit(OpCodes.Initobj, type); return this; }
        public ILEmitter newobj(ConstructorInfo ctor) { il.Emit(OpCodes.Newobj, ctor); return this; }
        public ILEmitter Throw() { il.Emit(OpCodes.Throw); return this; }
        public ILEmitter throw_new(Type type) { var exp = type.GetConstructor(Type.EmptyTypes); newobj(exp).Throw(); return this; }
        public ILEmitter throw_new(ConstructorInfo ctor) { newobj(ctor).Throw(); return this; }
        public ILEmitter stelem_ref() { il.Emit(OpCodes.Stelem_Ref); return this; }
        public ILEmitter ldelem_ref() { il.Emit(OpCodes.Ldelem_Ref); return this; }
        public ILEmitter ldlen() { il.Emit(OpCodes.Ldlen); return this; }
        public ILEmitter stloc(int idx) { il.Emit(OpCodes.Stloc, idx); return this; }
        public ILEmitter stloc_s(int idx) { il.Emit(OpCodes.Stloc_S, idx); return this; }
        public ILEmitter stloc(LocalBuilder local) { il.Emit(OpCodes.Stloc, local); return this; }
        public ILEmitter stloc_s(LocalBuilder local) { il.Emit(OpCodes.Stloc_S, local); return this; }
        public ILEmitter stloc_0() { il.Emit(OpCodes.Stloc_0); return this; }
        public ILEmitter stloc_1() { il.Emit(OpCodes.Stloc_1); return this; }
        public ILEmitter mark(Label label) { il.MarkLabel(label); return this; }
        public ILEmitter ldfld(FieldInfo field) { il.Emit(OpCodes.Ldfld, field); return this; }
        public ILEmitter ldsfld(FieldInfo field) { il.Emit(OpCodes.Ldsfld, field); return this; }
        public ILEmitter lodfld(FieldInfo field) { if (field.IsStatic) ldsfld(field); else ldfld(field); return this; }
        public ILEmitter ldflda(FieldInfo field) { il.Emit(OpCodes.Ldflda, field); return this; }
        public ILEmitter ifvaluetype_box(Type type) { if (type.IsValueType) il.Emit(OpCodes.Box, type); return this; }
        public ILEmitter stfld(FieldInfo field) { il.Emit(OpCodes.Stfld, field); return this; }
        public ILEmitter stsfld(FieldInfo field) { il.Emit(OpCodes.Stsfld, field); return this; }
        public ILEmitter setfld(FieldInfo field) { if (field.IsStatic) stsfld(field); else stfld(field); return this; }
        public ILEmitter unboxorcast(Type type) { if (type.IsValueType) unbox(type); else cast(type); return this; }
        public ILEmitter callorvirt(MethodInfo method) { if (method.IsVirtual) il.Emit(OpCodes.Callvirt, method); else il.Emit(OpCodes.Call, method); return this; }
        public ILEmitter stind_ref() { il.Emit(OpCodes.Stind_Ref); return this; }
        public ILEmitter ldind_ref() { il.Emit(OpCodes.Ldind_Ref); return this; }
        public LocalBuilder declocal(Type type) { return il.DeclareLocal(type); }
        public LocalBuilder declocal<T>() { return il.DeclareLocal(typeof(T)); }
        public Label deflabel() { return il.DefineLabel(); }
        public ILEmitter Switch(Label[] cases) { il.Emit(OpCodes.Switch, cases); return this; }
        public ILEmitter ifclass_ldarg_else_ldarga(int idx, Type type) { if (type.IsValueType) ldarga(idx); else ldarg(idx); return this; }
        public ILEmitter ifclass_ldloc_else_ldloca(int idx, Type type) { if (type.IsValueType) ldloca(idx); else ldloc(idx); return this; }
        public ILEmitter ifclass_ldloc_else_ldloca(LocalBuilder local, Type type) { if (type.IsValueType) ldloca(local); else ldloc(local); return this; }
        public ILEmitter ifclass_ldloc_else_ldloca_s(LocalBuilder local, Type type) { if (type.IsValueType) ldloca_s(local); else ldloc_s(local); return this; }
        public ILEmitter perform(Action<ILEmitter, MemberInfo> action, MemberInfo member) { action(this, member); return this; }
        public ILEmitter ifbyref_ldloca_else_ldloc(LocalBuilder local, Type type) { if (type.IsByRef) ldloca(local); else ldloc(local); return this; }
        public ILEmitter ldtoken(Type type) { il.Emit(OpCodes.Ldtoken, type); return this; }
        public ILEmitter brtrue(Label label) { il.Emit(OpCodes.Brtrue, label); return this; }
        public ILEmitter add() { il.Emit(OpCodes.Add); return this; }
        public ILEmitter br(Label label) { il.Emit(OpCodes.Br, label); return this; }
        public ILEmitter ldelem(Type type) { il.Emit(OpCodes.Ldelem, type); return this; }
        public ILEmitter conv_i4() { il.Emit(OpCodes.Conv_I4); return this; }
        public ILEmitter clt() { il.Emit(OpCodes.Clt); return this; }
        public ILEmitter brtrue_s(Label label) { il.Emit(OpCodes.Brtrue_S, label); return this; }
        public ILEmitter increment(LocalBuilder value) { ldloc(value); ldc_i4_1(); add(); stloc(value); return this; }
        public ILEmitter increment_s(LocalBuilder value) { ldloc_s(value); ldc_i4_1(); add(); stloc_s(value); return this; }
        public ILEmitter sub() { il.Emit(OpCodes.Sub); return this; }
        public ILEmitter newarr(Type type) { il.Emit(OpCodes.Newarr, type); return this; }
        public ILEmitter br_s(Label label) { il.Emit(OpCodes.Br_S, label); return this; }
        public ILEmitter ldelema(Type type) { il.Emit(OpCodes.Ldelema, type); return this; }
        public ILEmitter ldc_i4_s(int value) { il.Emit(OpCodes.Ldc_I4_S, value); return this; }
        public ILEmitter ceq() { il.Emit(OpCodes.Ceq); return this; }
        public ILEmitter brfalse_s(Label label) { il.Emit(OpCodes.Brfalse_S, label); return this; }
        public ILEmitter brfalse(Label label) { il.Emit(OpCodes.Brfalse, label); return this; }
        public ILEmitter bneq(Label label) { ceq(); brfalse(label); return this; }
        public ILEmitter bneq_s(Label label) { ceq(); brfalse_s(label); return this; }
        public ILEmitter blt(Label label) { il.Emit(OpCodes.Blt, label); return this; }
        public ILEmitter blt_s(Label label) { il.Emit(OpCodes.Blt_S, label); return this; }
        public ILEmitter stobj(Type type) { il.Emit(OpCodes.Stobj, type); return this; }
        public ILEmitter ldstr(string value) { il.Emit(OpCodes.Ldstr, value); return this; }
        public ILEmitter bge(Label label) { il.Emit(OpCodes.Bge, label); return this; }
        public ILEmitter bge_s(Label label) { il.Emit(OpCodes.Bge_S, label); return this; }
        public ILEmitter decrement(LocalBuilder value) { ldloc(value); ldc_i4_1(); sub(); stloc(value); return this; }
        public ILEmitter decrement_s(LocalBuilder value) { ldloc_s(value); ldc_i4_1(); sub(); stloc_s(value); return this; }
        public ILEmitter pop() { il.Emit(OpCodes.Pop); return this; }
        public ILEmitter stelem(Type type) { il.Emit(OpCodes.Stelem, type); return this; }
        public ILEmitter castas(Type type) { il.Emit(OpCodes.Isinst, type); return this; }
    }
}
