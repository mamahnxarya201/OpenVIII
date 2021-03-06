﻿using System;

namespace OpenVIII.Fields.Scripts.Instructions
{
    /// <summary>
    /// Global[index] = (UInt16)value;
    /// <para>Pop to memory (word)</para>
    /// <para>Pop value from stack and store the first two bytes (word) in Argument.</para>
    /// </summary>
    /// <see cref="http://wiki.ffrtt.ru/index.php?title=FF8/Field/Script/Opcodes/00D_POPM_W"/>
    public sealed class POPM_W : JsmInstruction
    {
        private GlobalVariableId<UInt16> _globalVariable;
        private IJsmExpression _value;

        public POPM_W(GlobalVariableId<UInt16> globalVariable, IJsmExpression value)
        {
            _globalVariable = globalVariable;
            _value = value;
        }

        public POPM_W(Int32 parameter, IStack<IJsmExpression> stack)
            : this(new GlobalVariableId<UInt16>(parameter),
                value: stack.Pop())
        {
        }

        public override String ToString()
        {
            return $"{nameof(POPM_W)}({nameof(_globalVariable)}: {_globalVariable}, {nameof(_value)}: {_value})";
        }

        public override void Format(ScriptWriter sw, IScriptFormatterContext formatterContext, IServices services)
        {
            FormatHelper.FormatGlobalSet(_globalVariable, _value, Jsm.GlobalUInt16, sw, formatterContext, services);
        }

        public override IAwaitable TestExecute(IServices services)
        {
            UInt16 value = (UInt16)_value.Calculate(services);
            ServiceId.Global[services].Set(_globalVariable, value);
            return DummyAwaitable.Instance;
        }
    }
}