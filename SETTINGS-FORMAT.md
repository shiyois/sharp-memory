# Settings Format

SharpMemory reads settings from:

```text
~/.sharp-memory/settings.json
```

Set `SHARPMEMORY_HOME` to override the default home directory.

For example, `SHARPMEMORY_HOME=C:/tools/sharp-memory` makes SharpMemory read:

```text
C:/tools/sharp-memory/settings.json
```

`settings.json` in the current working directory is still supported for compatibility.

Create the default file with:

```bash
sharp-memory init
```

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

If no repositories are configured and no `--repo` arguments are passed, SharpMemory starts with an empty repository list.

## Local File

`settings.json` is local machine configuration and should not be committed.

Runtime data is stored in:

```text
~/.sharp-memory/sharp-memory.db
```
