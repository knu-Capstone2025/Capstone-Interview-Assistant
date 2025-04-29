namespace InterviewAssistant.ApiService.Storage;

public interface IMemoryStorage
{
    /// <summary>
    /// 텍스트 콘텐츠를 저장합니다.
    /// </summary>
    /// <param name="key">저장 키</param>
    /// <param name="content">저장할 텍스트 내용</param>
    void StoreContent(string key, string content);

    /// <summary>
    /// 저장된 텍스트 콘텐츠를 가져옵니다.
    /// </summary>
    /// <param name="key">가져올 콘텐츠의 키</param>
    /// <returns>저장된 텍스트 내용</returns>
    string GetContent(string key);

    /// <summary>
    /// 지정된 키가 존재하는지 확인합니다.
    /// </summary>
    /// <param name="key">확인할 키</param>
    /// <returns>키 존재 여부</returns>
    bool ContainsKey(string key);
}
