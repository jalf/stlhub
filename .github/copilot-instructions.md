# Copilot Agent Instructions

- All commit messages must be written in English, regardless of the language used in the code or interface.
- All code comments (inline, block, XML, or documentation comments) must be written in English.
- Do not use Portuguese or any other language for commit messages or code comments, even if the rest of the project or user communication is in another language.
- If the user requests a commit or code comment in another language, politely remind them of this rule and proceed in English.

# C# and Avalonia Code Generation Guidelines

- Prefer idiomatic, modern C# (use properties, auto-properties, pattern matching, expression-bodied members, etc.).
- Use Avalonia best practices for MVVM: keep logic in ViewModels, use data binding, and avoid code-behind when possible.
- Always use `x:DataType` in AXAML for strong-typed bindings.
- Prefer `CompiledBinding` for performance and type safety.
- Use `partial` classes for code-behind only when UI logic cannot be handled in ViewModel.
- Name files, classes, and properties using PascalCase.
- Use clear, descriptive names for controls and bindings.
- For UI layout, prefer StackPanel, Grid, and DockPanel. Use Margin and Padding for spacing, not empty elements.
- Always test bindings and commands for null safety.
- When creating custom controls, use Avalonia's property system (StyledProperty/DirectProperty) and follow Avalonia documentation.
- For resources and theming, use DynamicResource and StaticResource appropriately.
- Keep XAML clean: remove unused namespaces and properties.
- Document public methods and classes with XML comments in English.
