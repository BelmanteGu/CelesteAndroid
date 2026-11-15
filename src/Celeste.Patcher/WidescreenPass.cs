using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CelesteAndroid.Patcher
{
	/// <summary>
	/// Widescreen, parte do patcher: troca as constantes de resolução embutidas no IL do Celeste por
	/// chamadas a <c>CelesteAndroid.Wide</c> (módulo de patches), que devolve o valor certo para a
	/// tela em uso. Com o modo desligado os métodos devolvem as constantes originais.
	/// <para>
	/// A troca é por método, numa lista explícita (<see cref="WidescreenRules"/>): o mesmo número
	/// aparece com outros sentidos (320 também é velocidade, 160 é velocidade e tamanho de buffer), e
	/// só quem leu o código sabe qual é qual. Corrotinas (IEnumerator) e lambdas viram tipos
	/// aninhados gerados pelo compilador; eles contam como parte do método que os criou.
	/// </para>
	/// </summary>
	public static class WidescreenPass
	{
		private const string WideType = "CelesteAndroid.Wide";

		private sealed class Substitution(string token, Func<Instruction, bool> matches, string method)
		{
			public string Token { get; } = token;
			public Func<Instruction, bool> Matches { get; } = matches;
			public string Method { get; } = method;
		}

		private static bool IsInt(Instruction i, int value) =>
			(i.OpCode == OpCodes.Ldc_I4 && i.Operand is int a && a == value)
			|| (i.OpCode == OpCodes.Ldc_I4_S && i.Operand is sbyte b && b == value);

		private static bool IsFloat(Instruction i, float value) =>
			i.OpCode == OpCodes.Ldc_R4 && i.Operand is float f && f == value;

		private static bool Calls(Instruction i, string type, string method) =>
			(i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)
			&& i.Operand is MethodReference m && m.Name == method && m.DeclaringType.FullName == type;

		private static readonly Substitution[] Substitutions =
		{
			new("i320", i => IsInt(i, 320), "IW"),
			new("f320", i => IsFloat(i, 320f), "FW"),
			new("i160", i => IsInt(i, 160), "IHalfW"),
			new("f160", i => IsFloat(i, 160f), "FHalfW"),
			new("f-160", i => IsFloat(i, -160f), "FNegHalfW"),
			new("f322", i => IsFloat(i, 322f), "FW2"),
			new("f330", i => IsFloat(i, 330f), "FW10"),
			new("f340", i => IsFloat(i, 340f), "FW20"),
			new("f352", i => IsFloat(i, 352f), "FW32"),
			new("i352", i => IsInt(i, 352), "IW32"),
			new("i1920", i => IsInt(i, 1920), "ITW"),
			new("f1920", i => IsFloat(i, 1920f), "FTW"),
			new("f960", i => IsFloat(i, 960f), "FHalfTW"),
			new("i1922", i => IsInt(i, 1922), "ITW2"),
			// Engine.ScreenMatrix (com o deslocamento da interface) → matriz do espaço largo.
			new("SM", i => i.OpCode == OpCodes.Ldsfld && i.Operand is FieldReference f && f.Name == "ScreenMatrix" && f.DeclaringType.FullName == "Monocle.Engine", "WorldMatrix"),
			// Engine.Width (largura da tela) → 1920, em código que posiciona a interface.
			new("EW", i => Calls(i, "Monocle.Engine", "get_Width"), "HudWidth"),
		};

		public static void Apply(ModuleDefinition module, Action<string> log)
		{
			TypeDefinition? wide = module.GetType(WideType);
			if (wide == null)
			{
				log($"Widescreen: {WideType} não está no módulo; nada a fazer.");
				return;
			}
			Dictionary<string, MethodReference> targets = Substitutions
				.Select(s => s.Method)
				.Distinct()
				.ToDictionary(name => name, name => module.ImportReference(wide.Methods.Single(m => m.Name == name && m.IsStatic)));

			int total = 0;
			foreach (WidescreenRules.Rule rule in WidescreenRules.All)
			{
				TypeDefinition? type = module.GetType(rule.Type);
				if (type == null)
				{
					log($"Widescreen: tipo não encontrado: {rule.Type}");
					continue;
				}
				Substitution[] subs = rule.Tokens
					.Select(t => Substitutions.FirstOrDefault(s => s.Token == t) ?? throw new ArgumentException($"Token desconhecido: {t} ({rule.Type})"))
					.ToArray();

				int count = 0;
				foreach (MethodDefinition method in MethodsOf(type, rule.Method))
				{
					foreach (Instruction instruction in method.Body.Instructions)
					{
						foreach (Substitution sub in subs)
						{
							if (sub.Matches(instruction))
							{
								// Mesmo objeto Instruction: desvios que apontam para ele continuam válidos.
								instruction.OpCode = OpCodes.Call;
								instruction.Operand = targets[sub.Method];
								count++;
								break;
							}
						}
					}
				}
				if (count == 0)
				{
					log($"Widescreen: nenhuma troca em {rule.Type}::{rule.Method} ({string.Join(' ', rule.Tokens)})");
				}
				total += count;
			}
			log($"Widescreen: {total} constantes trocadas");
		}

		/// <summary>Métodos do tipo com esse nome (ou todos, com "*"), incluindo corrotinas e lambdas.</summary>
		private static IEnumerable<MethodDefinition> MethodsOf(TypeDefinition type, string name)
		{
			foreach (MethodDefinition method in type.Methods)
			{
				if (method.HasBody && (name == "*" || method.Name == name))
				{
					yield return method;
				}
			}
			foreach (TypeDefinition nested in type.NestedTypes)
			{
				// <Routine>d__12 (corrotina), <>c__DisplayClass5_0 / <Method>b__5_0 (lambdas).
				bool generated = nested.Name.StartsWith('<');
				if (!generated)
				{
					// Classe aninhada declarada no código (MoveBlock.Border, TalkComponent.TalkComponentUI):
					// faz parte do tipo quando a regra é "*".
					if (name == "*")
					{
						foreach (MethodDefinition method in MethodsOf(nested, "*"))
						{
							yield return method;
						}
					}
					continue;
				}
				bool owned = name == "*" || nested.Name.StartsWith("<" + name + ">", StringComparison.Ordinal);
				foreach (MethodDefinition method in nested.Methods)
				{
					if (!method.HasBody)
					{
						continue;
					}
					if (owned || method.Name.StartsWith("<" + name + ">", StringComparison.Ordinal))
					{
						yield return method;
					}
				}
			}
			if (name != "*")
			{
				// Lambdas ficam no tipo <>c compartilhado, com o nome do método dono: <Metodo>b__3_0.
				foreach (MethodDefinition method in type.Methods)
				{
					if (method.HasBody && method.Name.StartsWith("<" + name + ">", StringComparison.Ordinal))
					{
						yield return method;
					}
				}
			}
		}
	}
}
