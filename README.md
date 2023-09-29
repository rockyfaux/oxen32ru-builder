# Утилита для сборки переводов игры Oxenfree 2 (bundle + tsv => bundle)

Представляет собой модифицированный код из [этого репозитория](https://github.com/kryakozebra/oxenfree2_rus_dev/tree/main/src/textrepack).

## Что требуется:

* Для компиляции требуется установить [.NET sdk](https://dotnet.microsoft.com/en-us/download/dotnet), потом выполнить команду:
```
dotnet build
```

* Файлы игры loc_packages_assets_.bundle и dialogue_packages_assets_all.bundle

* Файлы tsv таблицы переводов loc_packages_assets.tsv и dialogue_packages_assets_all.tsv.

## Пример запуска

Для замены одной из испанских локализаций:

```
trepack.exe -l es-419 -t nightly-yymmdd loc_packages_assets_.bundle loc_packages_assets.tsv

trepack.exe -l es-419 -t nightly-yymmdd dialogue_packages_assets_all.bundle dialogue_packages_assets_all.tsv

```

Для замены английской:

```
trepack.exe -l en -t nightly-yymmdd loc_packages_assets_.bundle loc_packages_assets.tsv

trepack.exe -l en -t nightly-yymmdd dialogue_packages_assets_all.bundle dialogue_packages_assets_all.tsv

```