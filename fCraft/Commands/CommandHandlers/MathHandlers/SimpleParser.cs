﻿// Copyright(C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

// Copyright(C) <2012> Lao Tszy(lao_tszy@yahoo.co.uk)

#region LICENCE
/*
Copyright 2017 video_error

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace fCraft.Commands.CommandHandlers.MathHandlers {

	// Shunting yard(Dijkstra) parser.
	public static class SimpleParser {

		private enum SpecialOperandKind {
			NotSpecial,
			LeftParenthesis,
			RightParenthesis,
			Comma
		}

		private struct FuncData {

			public IExpressionElement Func;

			public bool IsFunctionOrUnary;

			public int Precedence;

			public SpecialOperandKind SpecialKind;
		}

		static private Dictionary<string, FuncData> Functions;

		// Binary ops.
		private static Dictionary<char, FuncData> Operators;
		// Special ops.
		private static Dictionary<char, FuncData> SOperators;
		// Unary ops.
		private static Dictionary<char, FuncData> UOperators;

		private static Dictionary<string, IExpressionElement> Constants;

		static SimpleParser() {
			Functions = new Dictionary<string, FuncData> {{
				"sqrt", new FuncData() {
					Func = new Sqrt(), IsFunctionOrUnary = true
				}}, {
				"abs", new FuncData() {
					Func = new Abs(), IsFunctionOrUnary = true
				}}, {
				"sign", new FuncData() {
					Func = new Sign(), IsFunctionOrUnary = true
				}}, {
				"sq", new FuncData() {
					Func = new Sq(), IsFunctionOrUnary = true
				}}, {
				"exp", new FuncData() {
					Func = new Exp(), IsFunctionOrUnary = true
				}}, {
				"lg", new FuncData() {
					Func = new Lg(), IsFunctionOrUnary = true
				}}, {
				"ln", new FuncData() {
					Func = new Ln(), IsFunctionOrUnary = true
				}}, {
				"log", new FuncData() {
					Func = new Log(), IsFunctionOrUnary = true
				}}, {
				"sin", new FuncData() {
					Func = new Sin(), IsFunctionOrUnary = true
				}}, {
				"cos", new FuncData() {
					Func = new Cos(), IsFunctionOrUnary = true
				}}, {
				"tan", new FuncData() {
					Func = new Tan(), IsFunctionOrUnary = true
				}}, {
				"sinh", new FuncData() {
					Func = new Sinh(), IsFunctionOrUnary = true
				}}, {
				"cosh", new FuncData() {
					Func = new Cosh(), IsFunctionOrUnary = true
				}}, {
				"tanh", new FuncData() {
					Func = new Tanh(), IsFunctionOrUnary = true
				}}
			};

			// Special operators.
			SOperators = new Dictionary<char, FuncData> {{
				'(', new FuncData() {
					SpecialKind = SpecialOperandKind.LeftParenthesis
				}}, {
				')', new FuncData() {
					SpecialKind = SpecialOperandKind.RightParenthesis
				}}, {
				',', new FuncData() {
					SpecialKind = SpecialOperandKind.Comma
				}}
			};

			// "Normal" ops.
			Operators = new Dictionary<char, FuncData> {{
				'+', new FuncData() {
					Func = new Sum(), Precedence = 2
				}}, {
				'*', new FuncData() {
					Func = new Mul(), Precedence = 3
				}}, {
				'-', new FuncData() {
					Func = new Sub(), Precedence = 2
				}}, {
				'/', new FuncData() {
					Func = new Div(), Precedence = 3
				}}, {
				'%', new FuncData() {
					Func = new Mod(), Precedence = 3
				}}, {
				'^', new FuncData() {
					Func = new Pow(), Precedence = 4
				}}, {
				// Comparison ops can be used in both ways: as switches in expressions and as "main" ops of equalities and inequalities.
				'>', new FuncData() {
					Func = new Greater(), Precedence = 1
				}}, {
				'<', new FuncData() {
					Func = new Less(), Precedence = 1
				}}, {
				'=', new FuncData() {
					Func = new Equal(), Precedence = 1
				}}, {
				'&', new FuncData() {
					Func = new And(), Precedence = 0
				}}, {
				'|', new FuncData() {
					Func = new And(), Precedence = 0
				}},
			};

			// Etc.

			// Unary ops.
			// Negate, since it must be distinct from binary minus.
			UOperators = new Dictionary<char, FuncData> {{
				'-', new FuncData() {
					Func = new Negate(), IsFunctionOrUnary = true
				}}, {
				'!', new FuncData() {
					Func = new Not(), IsFunctionOrUnary = true
				}}
			};

			// Constants: e, pi.
			Constants = new Dictionary<string, IExpressionElement> {{
					"e", new E()
				}, {
					"pi", new Pi()
			}};
		}

		public static Expression Parse(string expression, IEnumerable<string> vars) {
			expression.ToLower();

			int pos = 0;

			Stack<FuncData> tmpStack = new Stack<FuncData>();

			Expression e = new Expression(vars);

			bool noBOperatorExpected = true;
			bool lParenthExpected = false;

			while(pos < expression.Length) {
				string term = ReadTerm(expression, ref pos);

				if(lParenthExpected && term != "(") {
					throw new ArgumentException("expected '(' at " + pos);
				}

				// Checked, can be turned off.
				lParenthExpected = false;

				// If a number, const, or variable, append to the expression.
				double num;

				// Number.
				if(Double.TryParse(term, out num)) {
					e.Append(new Variable() {
						Value = num, Name = num.ToString()
					});

					noBOperatorExpected = false;

					continue;
				}

				IExpressionElement el;

				// Constant.
				if(Constants.TryGetValue(term, out el)) {
					e.Append(el);

					noBOperatorExpected = false;

					continue;
				}

				Variable v;

				// Variable.
				if(e.Vars.TryGetValue(term, out v)) {
					e.Append(v);

					noBOperatorExpected = false;

					continue;
				}

				// If function -> push to stack.
				FuncData fd;

				if(Functions.TryGetValue(term, out fd)) {
					tmpStack.Push(fd);

					// Dont need to set/reset noBOperatorExpected.
					lParenthExpected = true;

					continue;
				}

				// Operators.
				if(term.Length == 1) {
					char termch = term[0];

					if(SOperators.TryGetValue(termch, out fd)) {
						switch(fd.SpecialKind) {
							case SpecialOperandKind.LeftParenthesis:
								tmpStack.Push(fd);

								noBOperatorExpected = true;

								break;
							
							case SpecialOperandKind.RightParenthesis:
								noBOperatorExpected = false;

								ProcessRightParenth(e, tmpStack, pos);

								break;
							
							case SpecialOperandKind.Comma:
								noBOperatorExpected = true;

								ProcessComma(e, tmpStack, pos);

								break;
						}

						continue;
					}

					if(noBOperatorExpected) {
						if(UOperators.TryGetValue(termch, out fd)) {
							tmpStack.Push(fd);

							//noBOperatorExpected = true; - is already true.
							continue;
						}
					} else {
						if(Operators.TryGetValue(termch, out fd)) {
							ProcessOperator(fd, e, tmpStack);

							noBOperatorExpected = true;

							continue;
						}
					}
				}

				// Unrecongnizable.
				throw new ArgumentException("unrecognizable term " + term + " at before " + pos);
			}

			// We will be tolerant to closing parenthesis, i.e. the left parenthesis left will be ignored.
			foreach(var f in tmpStack) {
				if(f.SpecialKind != SpecialOperandKind.LeftParenthesis) {
					e.Append(f.Func);
				} else {
					throw new ArgumentException("unmatching parenthesis");
				}
			}

			return e;
		}

		public static Expression ParseAsEquality(string expression, IEnumerable<string> vars) {
			Expression e = Parse(expression, vars);

			e.MakeEquality();

			return e;
		}

		// The input must be in the form of a=...
		public static string PreparseAssignment(ref string assignmentFunction) {
			int pos = 0;

			string varName = ReadTerm(assignmentFunction, ref pos);
			string assignment = ReadTerm(assignmentFunction, ref pos);

			if(assignment.Length > 1 || assignment[0] != '=') {
				throw new ArgumentException("the expression is not an assignment(i.e. not like z=...)");
			}

			assignmentFunction = assignmentFunction.Substring(pos);

			return varName;
		}

		private static void ProcessComma(Expression e, Stack<FuncData> tmpStack, int pos) {
			if(tmpStack.Count == 0) {
				throw new ArgumentException("comma without left parenthesis at " + pos);
			}

			while(tmpStack.Peek().SpecialKind != SpecialOperandKind.LeftParenthesis) {
				e.Append(tmpStack.Pop().Func);

				if(tmpStack.Count == 0) {
					throw new ArgumentException("comma without left parenthesis at " + pos);
				}
			}
		}

		private static void ProcessRightParenth(Expression e, Stack<FuncData> tmpStack, int pos) {
			if(tmpStack.Count == 0) {
				throw new ArgumentException("unmatching right parenthesis at " + pos);
			}

			while(tmpStack.Peek().SpecialKind != SpecialOperandKind.LeftParenthesis) {
				e.Append(tmpStack.Pop().Func);

				if(tmpStack.Count == 0) {
					throw new ArgumentException("unmatching right parenthesis at " + pos);
				}
			}

			// Remove left parenthesis.
			tmpStack.Pop();

			// Append function/unary minus or both.
			while(tmpStack.Count > 0 && tmpStack.Peek().IsFunctionOrUnary) {
				e.Append(tmpStack.Pop().Func);
			}
		}

		private static void ProcessOperator(FuncData op, Expression e, Stack<FuncData> tmpStack) {
			// While operators with higher or equal precedence.
			while(tmpStack.Count > 0) {
				FuncData fd = tmpStack.Peek();

				if(fd.SpecialKind == SpecialOperandKind.NotSpecial && !fd.IsFunctionOrUnary && fd.Precedence >= op.Precedence) {
					e.Append(tmpStack.Pop().Func);
				} else { 
					break;
				}
			}

			tmpStack.Push(op);
		}

		private static string ReadTerm(string s, ref int pos) {
			ReadSpaces(s, ref pos);

			char ch = s[pos];

			if(Operators.ContainsKey(ch) || SOperators.ContainsKey(ch) || UOperators.ContainsKey(ch) || /*tmp*/ ch == '=') {
				string ret = new string(s[pos], 1);

				++pos; 

				ReadSpaces(s, ref pos);

				return ret;
			}

			StringBuilder term = new StringBuilder();

			while(pos < s.Length) {
				// No support for exp number form!
				if(Char.IsLetterOrDigit(s[pos]) || s[pos] == '.') {
					term.Append(s[pos]);
				} else {
					break;
				}

				++pos;
			}

			ReadSpaces(s, ref pos);

			return term.ToString();
		}

		private static void ReadSpaces(string s, ref int pos) {
			while(pos < s.Length && (s[pos] == ' ' || s[pos] == '\t')) {
				++pos;
			}
		}
	}
}
