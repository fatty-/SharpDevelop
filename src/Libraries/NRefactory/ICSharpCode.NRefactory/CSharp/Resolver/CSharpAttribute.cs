﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[Serializable]
	public sealed class CSharpAttribute : Immutable, IAttribute
	{
		ITypeReference attributeType;
		DomRegion region;
		IList<IConstantValue> positionalArguments;
		IList<KeyValuePair<string, IConstantValue>> namedCtorArguments;
		IList<KeyValuePair<string, IConstantValue>> namedArguments;
		
		public CSharpAttribute(ITypeReference attributeType, DomRegion region,
		                       IList<IConstantValue> positionalArguments,
		                       IList<KeyValuePair<string, IConstantValue>> namedCtorArguments,
		                       IList<KeyValuePair<string, IConstantValue>> namedArguments)
		{
			if (attributeType == null)
				throw new ArgumentNullException("attributeType");
			this.attributeType = attributeType;
			this.region = region;
			this.positionalArguments = positionalArguments;
			this.namedCtorArguments = namedCtorArguments;
			this.namedArguments = namedArguments;
		}
		
		public DomRegion Region {
			get { return region; }
		}
		
		public ITypeReference AttributeType {
			get { return attributeType; }
		}
		
		public IMethod ResolveConstructor(ITypeResolveContext context)
		{
			CSharpResolver r = new CSharpResolver(context);
			IType type = attributeType.Resolve(context);
			int totalArgumentCount = 0;
			if (positionalArguments != null)
				totalArgumentCount += positionalArguments.Count;
			if (namedCtorArguments != null)
				totalArgumentCount += namedCtorArguments.Count;
			ResolveResult[] arguments = new ResolveResult[totalArgumentCount];
			string[] argumentNames = new string[totalArgumentCount];
			int i = 0;
			if (positionalArguments != null) {
				while (i < positionalArguments.Count) {
					IConstantValue cv = positionalArguments[i];
					arguments[i] = new ConstantResolveResult(cv.GetValueType(context), cv.GetValue(context));
					i++;
				}
			}
			if (namedCtorArguments != null) {
				foreach (var pair in namedCtorArguments) {
					argumentNames[i] = pair.Key;
					arguments[i] = new ConstantResolveResult(pair.Value.GetValueType(context), pair.Value.GetValue(context));
					i++;
				}
			}
			MemberResolveResult mrr = r.ResolveObjectCreation(type, arguments, argumentNames) as MemberResolveResult;
			return mrr != null ? mrr.Member as IMethod : null;
		}
		
		public IList<IConstantValue> GetPositionalArguments(ITypeResolveContext context)
		{
			if (namedCtorArguments == null || namedCtorArguments.Count == 0) {
				// no namedCtorArguments: just return the positionalArguments
				if (positionalArguments != null)
					return new ReadOnlyCollection<IConstantValue>(positionalArguments);
				else
					return EmptyList<IConstantValue>.Instance;
			}
			// we do have namedCtorArguments, which need to be re-ordered and appended to the positional arguments
			List<IConstantValue> result = new List<IConstantValue>(this.positionalArguments);
			IMethod method = ResolveConstructor(context);
			if (method != null) {
				for (int i = result.Count; i < method.Parameters.Count; i++) {
					IParameter p = method.Parameters[i];
					bool found = false;
					foreach (var pair in namedCtorArguments) {
						if (pair.Key == p.Name) {
							result.Add(pair.Value);
							found = true;
						}
					}
					if (!found) {
						// add the parameter's default value:
						result.Add(p.DefaultValue ?? new SimpleConstantValue(p.Type, CSharpResolver.GetDefaultValue(p.Type.Resolve(context))));
					}
				}
			}
			return result.AsReadOnly();
		}
		
		public IList<KeyValuePair<string, IConstantValue>> GetNamedArguments(ITypeResolveContext context)
		{
			if (namedArguments != null)
				return new ReadOnlyCollection<KeyValuePair<string, IConstantValue>>(namedArguments);
			else
				return EmptyList<KeyValuePair<string, IConstantValue>>.Instance;
		}
	}
	
	/// <summary>
	/// Type reference used within an attribute.
	/// Looks up both 'withoutSuffix' and 'withSuffix' and returns the type that exists.
	/// </summary>
	[Serializable]
	public sealed class AttributeTypeReference : ITypeReference, ISupportsInterning
	{
		ITypeReference withoutSuffix, withSuffix;
		
		public AttributeTypeReference(ITypeReference withoutSuffix, ITypeReference withSuffix)
		{
			if (withoutSuffix == null)
				throw new ArgumentNullException("withoutSuffix");
			if (withSuffix == null)
				throw new ArgumentNullException("withSuffix");
			this.withoutSuffix = withoutSuffix;
			this.withSuffix = withSuffix;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			// If both types exist, C# considers that to be an ambiguity, but we are less strict.
			IType type = withoutSuffix.Resolve(context);
			var attrType = context.GetTypeDefinition (typeof(System.Attribute));
			if (attrType == null)
				return SharedTypes.UnknownType;
			
			if (type.GetDefinition() == null || !type.GetDefinition().IsDerivedFrom(attrType, context))
				type = withSuffix.Resolve(context);
			
			return type;
		}
		
		public override string ToString()
		{
			return withoutSuffix.ToString() + "[Attribute]";
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			withoutSuffix = provider.Intern(withoutSuffix);
			withSuffix = provider.Intern(withSuffix);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				return withoutSuffix.GetHashCode() + 715613 * withSuffix.GetHashCode();
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			AttributeTypeReference atr = other as AttributeTypeReference;
			return atr != null && this.withoutSuffix == atr.withoutSuffix && this.withSuffix == atr.withSuffix;
		}
	}
}
