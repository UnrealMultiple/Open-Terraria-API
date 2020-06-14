/*
Copyright (C) 2020 DeathCradle

This file is part of Open Terraria API v3 (OTAPI)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Linq;
using System.Linq.Expressions;
using Mono.Cecil;
using MonoMod.Cil;

namespace OTAPI
{
    public static partial class Extensions
    {
        public static ILCursor GetILCursor(this MonoMod.MonoModder modder, Expression<Action> reference)
            => new ILCursor(new ILContext(modder.Module.GetDefinition<MethodDefinition>(reference)));

        public static FieldDefinition GetDefinition<TReturn>(this MonoMod.MonoModder modder, Expression<Func<TReturn>> reference)
            => modder.Module.GetDefinition<FieldDefinition>(reference);
        public static MethodDefinition GetDefinition(this MonoMod.MonoModder modder, Expression<Action> reference)
            => modder.Module.GetDefinition(reference);
        public static TypeDefinition GetDefinition<TType>(this MonoMod.MonoModder modder)
            => modder.Module.GetDefinition<TType>();

        public static ILCursor GetILCursor(this ModuleDefinition module, Expression<Action> reference)
            => new ILCursor(new ILContext(module.GetDefinition<MethodDefinition>(reference)));

        public static FieldDefinition GetDefinition<TReturn>(this ModuleDefinition module, Expression<Func<TReturn>> reference)
            => module.GetDefinition<FieldDefinition>(reference);
        public static MethodDefinition GetDefinition(this ModuleDefinition module, Expression<Action> reference)
            => module.GetDefinition<MethodDefinition>(reference);

        public static MethodReference GetReference<TReturn>(this ModuleDefinition module, Expression<Func<TReturn>> reference)
            => (MethodReference)module.GetMemberReference(reference);
        public static MemberReference GetReference(this ModuleDefinition module, Expression<Action> reference)
            => module.GetMemberReference(reference);

        public static TypeDefinition GetDefinition<TType>(this ModuleDefinition module)
        {
            var target = typeof(TType).FullName;
            return module.Types.Single(t => t.FullName == target);
        }

        public static TReturn GetDefinition<TReturn>(this IMetadataTokenProvider token, LambdaExpression reference)
                    => (TReturn)token.GetMemberReference(reference).Resolve();

        public static MemberReference GetMemberReference(this IMetadataTokenProvider token, LambdaExpression reference)
        {
            var module = (token as ModuleDefinition) ?? (token as AssemblyDefinition)?.MainModule;
            if (module != null)
            {
                // find the expression method in the meta, matching on the parameter count/types
                var mce = reference.Body as MethodCallExpression;
                if (mce != null)
                    return module.ImportReference(mce.Method);
            }

            var me = reference.Body as MemberExpression;
            if (me != null)
            {
                if (me.Member is System.Reflection.FieldInfo field)
                    return module.ImportReference(field);
            }

            throw new System.Exception($"Unable to find expression in assembly");
        }

        public static MethodReference GetCoreLibMethod(this ModuleDefinition module, string @namespace, string type, string method)
        {
            var type_ref = new TypeReference(@namespace, type,
                module.TypeSystem.String.Module,
                module.TypeSystem.CoreLibrary
            );
            return new MethodReference(method, module.TypeSystem.Void, type_ref);
        }
    }
}