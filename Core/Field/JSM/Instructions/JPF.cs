﻿using System;
using System.Collections.Generic;


namespace OpenVIII.Fields.Scripts.Instructions
{
    /// <summary>
    /// <para>Jump Forward with condition</para>
    /// <para>Jump forward a number of instructions given by the Argument if condition is equal to 0. Else does nothing. The condition is always popped from the stack.</para>
    /// </summary>
    /// <see cref="http://wiki.ffrtt.ru/index.php?title=FF8/Field/Script/Opcodes/003_JPF"/>
    public sealed class JPF : JsmInstruction, IJumpToOpcode, IFormattableScript
    {
        /// <summary>
        /// Number of instructions to jump forward. (in Deling's editor, this is just a label)
        /// </summary>
        public Int32 Offset { get; set; }
        public IReadOnlyList<IJsmExpression> Conditions => _conditions;
        private readonly List<IJsmExpression> _conditions = new List<IJsmExpression>();

        public JPF(Int32 offset, IJsmExpression condition)
        {
            Offset = offset;
            _conditions.Add(condition);
        }

        public JPF(Int32 offset, IStack<IJsmExpression> stack)
            : this(offset,
                condition: stack.Pop())
        {
        }

        public void Inverse(JMP nextJmp)
        {
            if (_conditions.Count != 1)
                throw new NotSupportedException($"Conditional jump already merged with an other one: {this}");

            IJsmExpression expression = _conditions[0];
            if (!(expression is ILogicalExpression constExpression))
                _conditions[0] = new Jsm.Expression.CAL.LogNot(expression);
            else
                _conditions[0] = constExpression.LogicalInverse();

            Int32 jmpIndex = nextJmp.Index;
            Int32 jmpOffset = nextJmp.Offset;
            nextJmp.Index = Index;
            nextJmp.Offset = Offset;
            Index = jmpIndex;
            Offset = jmpOffset;
        }

        public void Union(JPF newJpf)
        {
            foreach (var cond in newJpf.Conditions)
                _conditions.Add(cond);
        }

        private Int32 _index = -1;

        public Int32 Index
        {
            get
            {
                if (_index == -1)
                    throw new ArgumentException($"{nameof(JPF)} instruction isn't indexed yet.", nameof(Index));
                return _index;
            }
            set
            {
                //if (_index != -1)
                //    throw new ArgumentException($"{nameof(JPF)} instruction has already been indexed: {_index}.", nameof(Index));
                _index = value;
            }
        }

        public override String ToString()
        {
            return _index < 0
                ? $"{nameof(JPF)}[{nameof(Offset)}: {Offset}, {nameof(Conditions)}: ( {String.Join(") && (", Conditions)} )]"
                : $"{nameof(JPF)}[{nameof(Index)}: {Index}, {nameof(Conditions)}: ( {String.Join(") && (", Conditions)} )]";
        }

        public override void Format(ScriptWriter sw, IScriptFormatterContext formatterContext, IServices services)
        {
            ScriptWriter.State state = sw.RememberState();

            Boolean isTrue = true;
            foreach (IJsmExpression expression in _conditions)
            {
                if (expression is IConstExpression number)
                {
                    if (number.Value == 0)
                    {
                        // We can ignore the other conditions if one of them is always false
                        state.Cancel();
                        sw.Append("false");
                        return;
                    }

                    // We don't need to add a condition that is always true
                }
                else
                {
                    isTrue = false;

                    if (state.IsChanged)
                        sw.Append(" && ");

                    expression.Format(sw, formatterContext, services);
                }
            }

            if (isTrue)
            {
                // We can ignore conditions if all of them is always true
                state.Cancel();
                sw.Append("true");
            }
        }
    }
}