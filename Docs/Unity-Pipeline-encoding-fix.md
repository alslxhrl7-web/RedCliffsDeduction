# Unity Pipeline encoding fix

The project embeds com.unity.pipeline 0.7.0-exp.1 through Unity Package Manager.

Entering Play Mode raised `ExecutionEngineException: String conversion error: Illegal byte sequence` at `RuntimeAssembly.GetCodeBase`, called by `RuntimePipelineDriver.ShouldScanForCodeReload`.

The local patch reads the simple assembly name from `assembly.FullName` using `new AssemblyName(fullName).Name`, instead of `assembly.GetName().Name`. This preserves the assembly exclusion rules while avoiding CodeBase/path conversion. Dynamic assemblies are still skipped. Game and scene logic are unchanged.

When upgrading Pipeline, check whether the upstream implementation includes this fix before replacing the embedded package. Original package licenses and metadata are preserved.
