using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace AutoProxy
{
    public class AutoProxyFactory
    {
        public const string NAME = nameof(AutoProxyFactory);
        public const string DLL_NAME = nameof(AutoProxyFactory) + "Module.dll";

        protected Type BaseInvokerOfT;
        protected AssemblyName AseName;
        protected AssemblyBuilder AseBuilder;
        protected ModuleBuilder ModBuilder;

        public AutoProxyFactory(Type baseInvoker)
        {
            if (null == baseInvoker)
                throw new ArgumentNullException(nameof(baseInvoker));

            Type baseOfT = baseInvoker.GetInterface(typeof(IBaseAutoProxyInvoker<>).Name);
            bool bIsFromIBaseInvoker = null != baseOfT && 1 == baseOfT.GetGenericArguments().Length;
            if (!bIsFromIBaseInvoker)
                throw new NotSupportedException(Literals.BaseClassDoesNotImplementIBaseWcfInvokerOfT);

            this.BaseInvokerOfT = baseInvoker;
            this.AseName = new AssemblyName($"{NAME}Assembly");
            this.AseBuilder = AppDomain.CurrentDomain
                .DefineDynamicAssembly(this.AseName, AssemblyBuilderAccess.RunAndSave, "c:\\Temp\\");
            this.ModBuilder = this.AseBuilder
                .DefineDynamicModule($"{NAME}Module", DLL_NAME);
        }

        public void RegisterProxyBase<T>()
        {
        }

        public T CreateProxy<T>() where T: class
        {
            Type t = this.CreateProxyClassForType<T>();
            
            T response = (T)Activator.CreateInstance(t);
            return response;
        }

        public Type CreateProxyClassForType<T>() where T : class
        {
            Type tType = typeof(T);
            if (!tType.IsInterface)
                throw new NotSupportedException(Literals.TypeTMustBeInterface);
            if (!tType.IsPublic)
                throw new NotSupportedException(Literals.TypeTMustBePublic);

            Type baseOfT = this.BaseInvokerOfT;
            bool bIsFromIBaseInvoker = null != baseOfT.GetInterface(typeof(IBaseAutoProxyInvoker<T>).Name);
            if (!bIsFromIBaseInvoker)
                throw new NotSupportedException(Literals.BaseClassDoesNotImplementIBaseWcfInvokerOfT);

            TypeBuilder typeBuilder = this.ModBuilder.DefineType(
                $"{NAME}{tType.Name}", 
                TypeAttributes.Class,
                baseOfT,
                new Type[] { tType }
                );

            foreach(MethodInfo tMethod in tType.GetMethods())
            {
                bool bMethodReturns = typeof(void) != tMethod.ReturnType;

                if (bMethodReturns)
                    CreateReturnMethod(tType, baseOfT, typeBuilder, tMethod);
                else 
                    CreateVoidMethod(tType, baseOfT, typeBuilder, tMethod);
            }

            Type response = typeBuilder.CreateType();
            this.AseBuilder.Save(DLL_NAME);

            return response;
        }

        protected static void CreateReturnMethod(Type tType, Type baseOfT, TypeBuilder typeBuilder, MethodInfo tMethod)
        {
            ParameterInfo[] methodParameters = tMethod.GetParameters();

            Type[] methodParameterTypes = tMethod.GetParameters()
                .Select(s => s.ParameterType)
                .ToArray();

            // stworzenie DisplayClass
            TypeBuilder nestedTypeBuilder = typeBuilder
                .DefineNestedType($"{tMethod.Name}{NAME}DisplayClass", TypeAttributes.NestedPrivate);
            ConstructorBuilder nestedConstructor = nestedTypeBuilder
                .DefineDefaultConstructor(MethodAttributes.Public);
            FieldBuilder[] nestedFields = methodParameters
                .Select(metParam => nestedTypeBuilder.DefineField(metParam.Name, metParam.ParameterType, FieldAttributes.Public))
                .ToArray();

            MethodBuilder nestedMethodBuilder = nestedTypeBuilder.DefineMethod(
                tMethod.Name,
                MethodAttributes.Public,
                tMethod.ReturnType,
                new Type[] { tType }
                );

            ConstructorInfo argumentNullExceptionCtor = typeof(ArgumentNullException)
                .GetConstructor(new Type[] { typeof(string) });

            ILGenerator nil = nestedMethodBuilder.GetILGenerator();
            Label notNullLabel = nil.DefineLabel();
            nil.Emit(OpCodes.Ldarg_1);
            nil.Emit(OpCodes.Ldnull);
            nil.Emit(OpCodes.Ceq);
            nil.Emit(OpCodes.Brfalse, notNullLabel);
            nil.Emit(OpCodes.Ldstr, "proxy");
            nil.Emit(OpCodes.Newobj, argumentNullExceptionCtor);
            nil.Emit(OpCodes.Throw);

            nil.MarkLabel(notNullLabel);
            nil.Emit(OpCodes.Ldarg_1);
            foreach (FieldBuilder nesField in nestedFields)
            {
                nil.Emit(OpCodes.Ldarg_0);
                nil.Emit(OpCodes.Ldfld, nesField);
            }
            nil.EmitCall(OpCodes.Callvirt, tMethod, new Type[0]);
            nil.Emit(OpCodes.Ret);

            Type n = nestedTypeBuilder.CreateType();

            // stworzenie metody
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
            tMethod.Name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.Final,
            tMethod.ReturnType,
            methodParameterTypes
            );

            ILGenerator il = methodBuilder.GetILGenerator();
            il.DeclareLocal(nestedTypeBuilder);
            il.DeclareLocal(tMethod.ReturnType);
            il.Emit(OpCodes.Newobj, nestedConstructor);
            il.Emit(OpCodes.Stloc_0);

            int iMetodArg = 1;
            foreach (FieldBuilder nesField in nestedFields)
            {
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldarg, iMetodArg++);
                il.Emit(OpCodes.Stfld, nesField);
            }
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldftn, nestedMethodBuilder);
            Type funcType = typeof(Func<,>);
            Type funcOfTU = funcType.MakeGenericType(tType, tMethod.ReturnType);
            ConstructorInfo funcCtor = funcOfTU.GetConstructor(new Type[] { typeof(object), typeof(IntPtr) });
            il.Emit(OpCodes.Newobj, funcCtor);
            MethodInfo invokeReturn = baseOfT
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(w => w.Name == "Invoke" && w.ReturnType != typeof(void))
                .First();
            il.EmitCall(OpCodes.Call, invokeReturn.MakeGenericMethod(tMethod.ReturnType), null);
            il.Emit(OpCodes.Stloc_1);

            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ret);
        }

        protected static void CreateVoidMethod(Type tType, Type baseOfT, TypeBuilder typeBuilder, MethodInfo tMethod)
        {
            ParameterInfo[] methodParameters = tMethod.GetParameters();

            Type[] methodParameterTypes = tMethod.GetParameters()
                .Select(s => s.ParameterType)
                .ToArray();

            // stworzenie DisplayClass
            TypeBuilder nestedTypeBuilder = typeBuilder
                .DefineNestedType($"{tMethod.Name}{NAME}DisplayClass", TypeAttributes.NestedPrivate);
            ConstructorBuilder nestedConstructor = nestedTypeBuilder
                .DefineDefaultConstructor(MethodAttributes.Public);
            FieldBuilder[] nestedFields = methodParameters
                .Select(metParam => nestedTypeBuilder.DefineField(metParam.Name, metParam.ParameterType, FieldAttributes.Public))
                .ToArray();

            MethodBuilder nestedMethodBuilder = nestedTypeBuilder.DefineMethod(
                tMethod.Name,
                MethodAttributes.Public,
                tMethod.ReturnType,
                new Type[] { tType }
                );

            ConstructorInfo argumentNullExceptionCtor = typeof(ArgumentNullException)
                .GetConstructor(new Type[] { typeof(string) });

            ILGenerator nil = nestedMethodBuilder.GetILGenerator();
            Label notNullLabel = nil.DefineLabel();
            nil.Emit(OpCodes.Ldarg_1);
            nil.Emit(OpCodes.Ldnull);
            nil.Emit(OpCodes.Ceq);
            nil.Emit(OpCodes.Brfalse, notNullLabel);
            nil.Emit(OpCodes.Ldstr, "proxy");
            nil.Emit(OpCodes.Newobj, argumentNullExceptionCtor);
            nil.Emit(OpCodes.Throw);

            nil.MarkLabel(notNullLabel);
            nil.Emit(OpCodes.Ldarg_1);
            foreach (FieldBuilder nesField in nestedFields)
            {
                nil.Emit(OpCodes.Ldarg_0);
                nil.Emit(OpCodes.Ldfld, nesField);
            }
            nil.EmitCall(OpCodes.Callvirt, tMethod, new Type[0]);
            nil.Emit(OpCodes.Ret);

            Type n = nestedTypeBuilder.CreateType();

            // stworzenie metody
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
            tMethod.Name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.Final,
            tMethod.ReturnType,
            methodParameterTypes
            );

            ILGenerator il = methodBuilder.GetILGenerator();
            il.DeclareLocal(nestedTypeBuilder);
            //il.DeclareLocal(tMethod.ReturnType);
            il.Emit(OpCodes.Newobj, nestedConstructor);
            il.Emit(OpCodes.Stloc_0);

            int iMetodArg = 1;
            foreach (FieldBuilder nesField in nestedFields)
            {
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldarg, iMetodArg++);
                //il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, nesField);
            }
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldftn, nestedMethodBuilder);
            Type actionType = typeof(Action<>);
            Type actionOfT = actionType.MakeGenericType(tType);
            ConstructorInfo actionCtor = actionOfT.GetConstructor(new Type[] { typeof(object), typeof(IntPtr) });
            il.Emit(OpCodes.Newobj, actionCtor);
            MethodInfo invokeVoid = baseOfT
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(w => w.Name == "Invoke" && w.ReturnType == typeof(void))
                .First();
            il.EmitCall(OpCodes.Call, invokeVoid, null);
            
            il.Emit(OpCodes.Ret);

        }
    }
}
