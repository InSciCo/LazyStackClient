# LazyStack.FactoryGenerator

This project generates factory class code for classes annotated with the [Factory] annotation.



## Using this Generator 
In the project you want to generate code for, add a reference to the LazyStack.Generator project.
```<ProjectReference Include="..\..\LazyStackClient\LazyStack.TreeViewModel\LazyStack.TreeViewModel.csproj" OutputItemType="Analyzer" />```

Note the OutputItemType attribute. This is required to make the generator work.

## Notes
The generator TargetFramework is 'netstandard2.0'. This is required to make the generator work.

