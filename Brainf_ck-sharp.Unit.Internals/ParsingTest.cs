﻿using Brainf_ck_sharp.NET;
using Brainf_ck_sharp.NET.Buffers;
using Brainf_ck_sharp.NET.Constants;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Brainf_ck_sharp.Unit.Internals
{
    /// <summary>
    /// A test <see langword="class"/> to test internal APIs from the <see cref="Brainf_ckParser"/> <see langword="class"/>
    /// </summary>
    [TestClass]
    public class ParsingTest
    {
        [TestMethod]
        public void ExtractSource()
        {
            using UnsafeMemoryBuffer<byte> operators = UnsafeMemoryBuffer<byte>.Allocate(11, false);

            operators[0] = Operators.Plus;
            operators[1] = Operators.Minus;
            operators[2] = Operators.ForwardPtr;
            operators[3] = Operators.BackwardPtr;
            operators[4] = Operators.PrintChar;
            operators[5] = Operators.ReadChar;
            operators[6] = Operators.LoopStart;
            operators[7] = Operators.LoopEnd;
            operators[8] = Operators.FunctionStart;
            operators[9] = Operators.FunctionEnd;
            operators[10] = Operators.FunctionCall;

            string source = Brainf_ckParser.ExtractSource(operators.Memory);

            Assert.IsNotNull(source);
            Assert.AreEqual(source, "+-><.,[]():");
        }

        [TestMethod]
        public void TryParse1()
        {
            const string script = "[\n\tTest script\n]\n+++++[\n\t>++ 5 x 2 = 10\n\t<- Loop decrement\n]\n> Move to cell 1";

            using UnsafeMemoryBuffer<byte> operators = Brainf_ckParser.TryParse(script, out _);

            Assert.IsNotNull(operators);
            Assert.AreEqual(operators.Size, 15);

            string source = Brainf_ckParser.ExtractSource(operators.Memory);

            Assert.IsNotNull(source);
            Assert.AreEqual(source, "[]+++++[>++<-]>");
        }
    }
}