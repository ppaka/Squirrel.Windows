| README.md |
|:---|

## Unity를 위한 Squirrel
Squirrel은 .dll 위치에 따라 작동하는 방식이고
C#프로젝트와는 달리
유니티는 (게임폴더)\(게임이름)_Data\Managed 폴더에 dll이 저장됩니다.
원본 프로젝트의 소스중 UpdateManager.cs를 수정했습니다.
update.exe를 정상적으로 찾을 수 있습니다.

## Squirrel 빌드하기

```sh
git clone --recursive https://github.com/ppaka/Squirrel.Windows-for-Unity
cd squirrel.windows
.\.NuGet\NuGet.exe restore
msbuild /p:Configuration=Release
```

This is an unofficial fork of Squirrel.Windows.

Modified UpdateManager.cs for Unity's Mono scripting backend.
