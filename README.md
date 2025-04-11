# Capstone Interview Assistant

Azure OpenAI 및 Semantic Kernel을 활용한 인터뷰 코치 앱입니다

## 시스템 아키텍처

![전체 시스템 아키텍처](./images/architecture.png)

## 애플리케이션 구조

```text
InterviewAssistance
├── src
│   ├── InterviewAssistance.AppHost
│   ├── InterviewAssistance.ServiceDefaults
│   ├── InterviewAssistance.Web
│   ├── InterviewAssistance.ApiService
│   └── InterviewAssistance.Common
└── test
    ├── InterviewAssistance.AppHost.Tests
    ├── InterviewAssistance.Web.Tests
    ├── InterviewAssistance.ApiService.Tests
    └── InterviewAssistance.Common.Tests
```

## 프로젝트 의존성

```text
InterviewAssistance
├── src
│   └── InterviewAssistance.AppHost
│       ├── InterviewAssistance.Web
│       │   ├── InterviewAssistance.ServiceDefaults
│       │   └── InterviewAssistance.Common
│       └── InterviewAssistance.ApiService
│           ├── InterviewAssistance.ServiceDefaults
│           └── InterviewAssistance.Common
└── test
    ├── InterviewAssistance.AppHost.Tests
    │   └── InterviewAssistance.AppHost
    ├── InterviewAssistance.Web.Tests
    │   └── InterviewAssistance.Web
    ├── InterviewAssistance.ApiService.Tests
    │   └── InterviewAssistance.ApiService
    └── InterviewAssistance.Common.Tests
        └── InterviewAssistance.Common
```

## 사전 준비사항

- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0) 설치
- [Visual Studio Code](https://code.visualstudio.com/) 설치
- [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) 설치
- [git CLI](https://git-scm.com/downloads) 설치
- [GitHub CLI](https://cli.github.com/) 설치
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) 설치
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) 설치
- [Docker Desktop](https://docs.docker.com/get-started/introduction/get-docker-desktop/) 설치

## 시작하기

### GitHub Model 사용을 위한 Personal Access Token 생성

1. [Personal Access Token 관리](https://docs.github.com/ko/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#personal-access-token-classic-%EB%A7%8C%EB%93%A4%EA%B8%B0) 페이지 참조해서 GitHub PAT 생성

### 로컬 실행

1. 아래 명령어 실행

    ```bash
    dotnet user-secrets --project ./src/InterviewAssistant.AppHost set ConnectionStrings:openai "Endpoint=https://models.inference.ai.azure.com;Key={{GITHUB_PAT}}"
    ```

   > `{{GITHUB_PAT}}`은 앞서 생성한 GitHub PAT 값

1. 아래 명령어 실행

    ```bash
    dotnet watch run --project ./src/InterviewAssistant.AppHost
    ```

1. .NET Aspire 대시보드 나타나면 `webfrontend` 클릭해서 앱 실행
1. 화면 지시대로 이력서 및 구인공고 파일 업로드한 후 계속 진행

### Azure 클라우드 배포

1. 아래 명령어 실행

    ```bash
     azd auth login
    ```

1. 아래 명령어 실행

    ```bash
    azd up
    ```

   -  environment name 물어볼 경우 아무 값이나 입력 👉 예) `knu-interview-assistant`
   -  openai 커넥션 스트링을 물어볼 경우 👉 `Endpoint=https://models.inference.ai.azure.com;Key={{GITHUB_PAT}}` 입력 `{{GITHUB_PAT}}`은 앞서 생성한 GitHub PAT 값

1. 배포가 끝난 후 `webfrontend` 애플리케이션 URL 클릭하여 앱 실행
1. 화면 지시대로 이력서 및 구인공고 파일 업로드한 후 계속 진행
