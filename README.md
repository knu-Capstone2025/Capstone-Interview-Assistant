# Capstone Interview Assistant
Azure OpenAI 및 Semantic Kernel을 활용한 인터뷰 코치 앱입니다

## 시스템 아키텍처
![전체 시스템 아키텍처](./images/architecture.png)

## 애플리케이션 구조
```text
InterviewAssistant
├── src
│   ├── InterviewAssistant.AppHost
│   ├── InterviewAssistant.ServiceDefaults
│   ├── InterviewAssistant.Web
│   ├── InterviewAssistant.ApiService
│   ├── InterviewAssistant.Common
│   └── InterviewAssistant.McpMarkItDown
│
└── test
    ├── InterviewAssistant.AppHost.Tests
    ├── InterviewAssistant.Web.Tests
    ├── InterviewAssistant.ApiService.Tests
    └── InterviewAssistant.Common.Tests
```

## 프로젝트 의존성
```text
InterviewAssistant
├── src
│   └── InterviewAssistant.AppHost
│       ├── InterviewAssistant.Web
│       │   ├── InterviewAssistant.ServiceDefaults
│       │   └── InterviewAssistant.Common
│       ├── InterviewAssistant.ApiService
│       │   ├── InterviewAssistant.ServiceDefaults
│       │   └── InterviewAssistant.Common
│       └── InterviewAssistant.McpMarkItDown
└── test
    ├── InterviewAssistant.AppHost.Tests
    │   └── InterviewAssistant.AppHost
    ├── InterviewAssistant.Web.Tests
    │   └── InterviewAssistant.Web
    ├── InterviewAssistant.ApiService.Tests
    │   └── InterviewAssistant.ApiService
    └── InterviewAssistant.Common.Tests
        └── InterviewAssistant.Common
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

### .NET SDK 설치
Windows의 경우:
```bash
# winget을 통한 설치
winget install Microsoft.DotNet.SDK.9
```

macOS의 경우:
```bash
# Homebrew를 통한 설치
brew install dotnet
```

Linux (Ubuntu)의 경우:
```bash
# Microsoft 패키지 저장소 추가
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# .NET SDK 설치
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

### Azure CLI 설치
Windows의 경우:
```bash
# winget을 통한 설치
winget install -e --id Microsoft.AzureCLI
```

macOS의 경우:
```bash
# Homebrew를 통한 설치
brew install azure-cli
```

Linux의 경우:
```bash
# 설치 스크립트 실행
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

### Azure Developer CLI 설치
Windows의 경우:
```bash
# winget을 통한 설치
winget install microsoft.azd
```

macOS의 경우:
```bash
# Homebrew를 통한 설치
brew tap azure/azd && brew install azd
```

Linux의 경우:
```bash
# 설치 스크립트 실행
curl -fsSL https://aka.ms/install-azd.sh | bash
```

## 초기 설정

### McpMarkItDown 설치
1. 프로젝트 루트 디렉토리에서 다음 명령어를 실행하여 폴더를 생성합니다:
   ```bash
   mkdir -p src/InterviewAssistant.McpMarkItDown
   ```

2. 생성한 폴더로 이동한 후 MarkItDown을 클론합니다:
   ```bash
   cd src/InterviewAssistant.McpMarkItDown
   git clone https://github.com/microsoft/markitdown .
   ```

### Application Insights 환경변수 설정
다음 명령어를 실행합니다:
```bash
dotnet user-secrets --project ./src/InterviewAssistant.AppHost set ConnectionStrings:applicationinsights "InstrumentationKey=yourvalue;IngestionEndpoint=https://koreacentral-1.in.applicationinsights.azure.com/;LiveEndpoint=https://koreacentral.livediagnostics.monitor.azure.com/;ApplicationId=yourvalue"
```

> **참고**: Application Insights를 사용하려는 경우 `yourvalue` 부분을 실제 값으로 교체하세요. 사용하지 않는 경우 위 명령어를 그대로 실행해도 무관합니다.

## 시작하기

### GitHub Model 사용을 위한 Personal Access Token 생성
1. [Personal Access Token 관리](https://docs.github.com/ko/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#personal-access-token-classic-%EB%A7%8C%EB%93%A4%EA%B8%B0) 페이지 참조해서 GitHub PAT 생성

### 로컬 실행
1. 아래 명령어 실행
    ```bash
    dotnet user-secrets --project ./src/InterviewAssistant.AppHost set ConnectionStrings:openai "Endpoint=https://models.inference.ai.azure.com;Key={{GITHUB_PAT}}"
    ```
   > `{{GITHUB_PAT}}`은 앞서 생성한 GitHub PAT 값

2. 아래 명령어 실행
    ```bash
    dotnet watch run --project ./src/InterviewAssistant.AppHost
    ```

3. .NET Aspire 대시보드 나타나면 `webfrontend` 클릭해서 앱 실행

4. 화면 지시대로 이력서 및 구인공고 파일 업로드한 후 계속 진행

### Azure 클라우드 배포
1. 아래 명령어 실행
    ```bash
     azd auth login
    ```

2. 아래 명령어 실행
    ```bash
    azd up
    ```
   -  environment name 물어볼 경우 아무 값이나 입력 👉 예) `knu-interview-assistant`
   -  openai 커넥션 스트링을 물어볼 경우 👉 `Endpoint=https://models.inference.ai.azure.com;Key={{GITHUB_PAT}}` 입력 `{{GITHUB_PAT}}`은 앞서 생성한 GitHub PAT 값

3. 배포가 끝난 후 `webfrontend` 애플리케이션 URL 클릭하여 앱 실행

4. 화면 지시대로 이력서 및 구인공고 파일 업로드한 후 계속 진행