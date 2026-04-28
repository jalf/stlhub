# Agent Instructions

## Development Environment

The code is developed in a **Windows** environment. Therefore, all automated scripts, build commands, deployment, or utility scripts must be written in **PowerShell**.

**All documentation must be written in English.**

# Build and Error Checking Rules

After completing any task, always perform a build to check for compilation errors.
If there are issues during application startup, check the `fatal.log` file for startup errors and address them as needed.

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
