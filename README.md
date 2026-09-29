<p align="center">
  <img src="assets/logo/icon_128.png" width="96" alt="Acorda"/>
</p>

<h1 align="center">Acorda</h1>

<p align="center">
  Widget de afazeres pra área de trabalho do Windows.
</p>

## O que é

Uma lista de tarefas simples que fica ancorada no seu desktop estilo Widget.

## Instalação

Baixe o instalador mais recente em [Releases](https://github.com/caiojohnston/acorda/releases), rode o `Acorda-Setup.exe` e siga o assistente.

## Build a partir do código

Requer [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet build src/Acorda
```

Pra gerar o executável e o instalador:

```bash
dotnet publish src/Acorda -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

```bash
"C:\Users\<voce>\AppData\Local\Programs\Inno Setup 6\ISCC.exe" installer\acorda.iss
```

O instalador final fica em `dist/Acorda-Setup.exe` ([Inno Setup](https://jrsoftware.org/isinfo.php) é necessário só pra esse passo).

## Estrutura do projeto

```
src/Acorda/         # app WPF (.NET 8)
  Models/            # entidades e enums (tarefa, tema, tamanho, settings)
  Services/          # persistência (JSON), regra de negócio, startup no Windows
  ViewModels/         # view models da lista e dos gráficos
  Native/            # P/Invoke pra ancorar no desktop (WorkerW)
  Themes/            # dicionários de recursos por tema
installer/           # script do Inno Setup
tools/icongen/       # gerador do ícone (fonte Lexend -> .ico multi-tamanho)
assets/              # fonte Lexend (OFL) e ícone gerado
```

## Stack

C# / WPF (.NET 8), [Hardcodet.NotifyIcon.Wpf](https://github.com/HavenDV/H.NotifyIcon) pro ícone de bandeja, persistência em JSON local (`%AppData%\Acorda`).

## Licença

Fonte [Lexend](https://github.com/googlefonts/lexend) sob [SIL Open Font License](assets/fonts/OFL.txt), usada no logo.
