using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using ServerlessMoedas.Models;
using Dapper;

namespace ServerlessMoedas
{
    public static class MoedasQueueTrigger
    {
        [FunctionName("MoedasQueueTrigger")]
        public static void Run([QueueTrigger("queue-cotacoes", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            var cotacao =
                JsonSerializer.Deserialize<Cotacao>(myQueueItem);

            if (!String.IsNullOrWhiteSpace(cotacao.Sigla) &&
                cotacao.Valor.HasValue && cotacao.Valor > 0)
            {
                using (var conexao = new SqlConnection(
                    Environment.GetEnvironmentVariable("BaseCotacoes")))
                {

                    if (conexao.QueryFirst<int>(
                        "SELECT 1 FROM dbo.Cotacoes WHERE Sigla = @SiglaCotacao",
                        new { SiglaCotacao = cotacao.Sigla }) == 1)
                    {
                        conexao.Execute("UPDATE dbo.Cotacoes SET " +
                            "Valor = @ValorCotacao, " +
                            "UltimaCotacao = GETDATE() " +
                            "WHERE Sigla = @SiglaCotacao",
                            new
                            {
                                ValorCotacao = cotacao.Valor,
                                SiglaCotacao = cotacao.Sigla
                            });
                    }
                }

                log.LogInformation($"MoedasQueueTrigger: {myQueueItem}");
            }
            else
                log.LogError($"MoedasQueueTrigger - Erro validação: {myQueueItem}");
        }
    }
}