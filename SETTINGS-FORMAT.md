# Settings Format

SharpMemory uses `settings.json` from the project root.

Today the file may be absent for compatibility. This is legacy behavior: future versions are expected to require `settings.json`.

## Shape

```json
{
  "repositories": [
    "/absolute/path/to/repo"
  ]
}
```

## `repositories`

Required array of repository paths.

Use absolute paths.

`--repo <path>` is a temporary override. It can be repeated and takes precedence over `settings.json`.

Current fallback: if no repositories are configured and no `--repo` arguments are passed, SharpMemory indexes the current project root. This fallback should not be treated as the long-term contract.

## Local File

`settings.json` is local machine configuration and should not be committed.
