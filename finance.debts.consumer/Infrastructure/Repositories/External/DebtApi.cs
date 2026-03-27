namespace finance.debts.consumer.Infrastructure.Repositories.External
{
    public class DebtApi
    {
        private readonly HttpClient _httpClient;

        public DebtApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> ProcessDebtAsync(int debtId, string correlationId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Debts/{debtId}/process");

            request.Headers.Add("x-correlation-id", correlationId);

            var response = await _httpClient.SendAsync(request);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode >= 500)
                    throw new HttpRequestException("Erro externo");

                throw new Exception(content);
            }

            return content;
        }
    }
}
