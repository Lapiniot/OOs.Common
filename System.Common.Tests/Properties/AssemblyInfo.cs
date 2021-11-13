global using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.MethodLevel)]