# 스크립트

Azure OpenAI 생성/삭제 관련 다양한 스크립트를 모아뒀습니다.

## 사전 요구사항

- [PowerShell 7.5](https://learn.microsoft.com/ko-kr/powershell/scripting/install/installing-powershell?view=powershell-7.5) 이상

## `New-OpenAIs.ps1`

Azure OpenAI 인스턴스를 모든 가용한 지역에 지정한 모델로 생성합니다 (문서 작성 시점 총 24지역). 기본값으로 `GPT-4o`, `2024-11-20` 모델을 생성합니다.

```powershell
./scripts/New-OpenAIs.ps1 -AzureEnvironmentName {{환경명}}
```

## `Get-OpenAIDetails.ps1`

`New-OpenAIs.ps1`로 생성한 모든 Azure OpenAI 인스턴스의 엔드포인트와 API키를 `scripts/instances.json` 파일로 저장합니다.

```powershell
./scripts/Get-OpenAIDetails.ps1 -AzureEnvironmentName {{환경명}}
```

## `Purge-CognitiveServices.ps1`

Azure OpenAI 인스턴스는 삭제해도 한번에 삭제되지 않으므로 이 스크립트를 이용해 완전히 삭제합니다.

```powershell
./scripts/Purge-CognitiveServices.ps1
```
