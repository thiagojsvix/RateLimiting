# O que é Rate Limiting? 

A Rate Limiting é o conceito de limitar o quanto um recurso pode ser acessado. Por exemplo, você sabe que um banco de dados acessado pelo seu aplicativo pode lidar com 1.000 requests por minuto com segurança, mas não tem certeza de que ele possa lidar com muito mais do que isso. Você pode colocar um limitador de taxa em seu aplicativo que permita 1.000 requests a cada minuto e rejeitar as requests adicionais antes que elas possam acessar o banco de dados. Assim, você evite que requests em demasia possam acessar o seu banco de dados e permita que seu aplicativo lide com um número seguro de requests sem potencialmente causar falhas graves em seu banco de dados.

Existem vários algoritmos de limitação de taxa diferentes para controlar o fluxo de solicitações. Veremos quatro deles que serão fornecidos apartir do .NET 7.

## Concurrency limit

O limitador de simultaneidade limita quantas solicitações simultâneas podem acessar um recurso. Se o seu limite for 10, então 10 solicitações poderão acessar um recurso de uma vez e a 11ª solicitação não será permitida. Depois que uma solicitação é concluída, o número de solicitações permitidas aumenta para 1, quando uma segunda solicitação é concluída, o número aumenta para 2, etc. Isso é feito descartando um RateLimitLease sobre o qual falaremos mais tarde.

O código a seguir usa o limitador de simultaneidade:

```csharp
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using WebRateLimitAuth.Models;

var builder = WebApplication.CreateBuilder(args);

var concurrencyPolicy = "Concurrency";
var myOptions = new MyRateLimitOptions();
builder.Configuration.GetSection(MyRateLimitOptions.MyRateLimit).Bind(myOptions);

builder.Services.AddRateLimiter(_ => _
    .AddConcurrencyLimiter(policyName: concurrencyPolicy, options =>
    {
        options.PermitLimit = myOptions.PermitLimit;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = myOptions.QueueLimit;
    }));

var app = builder.Build();

app.UseRateLimiter();

static string GetTicks() => (DateTime.Now.Ticks & 0x11111).ToString("00000");

app.MapGet("/", async () =>
{
    await Task.Delay(500);
    return Results.Ok($"Concurrency Limiter {GetTicks()}");
                              
}).RequireRateLimiting(concurrencyPolicy);

app.Run();
```

## Token bucket limit

Token bucket é um algoritmo cujo nome deriva da descrição de como funciona. Imagine que há um balde cheio até a borda com fichas. Quando uma solicitação chega, ela pega um token e o guarda para sempre. Após algum período de tempo consistente, alguém adiciona um número predeterminado de tokens de volta ao balde, nunca adicionando mais do que o balde pode conter. Se o bucket estiver vazio, quando uma solicitação chegar, o acesso ao recurso será negado à solicitação.

Para dar um exemplo mais concreto, digamos que o balde pode conter 10 tokens e a cada minuto 2 tokens são adicionados ao balde. Quando uma solicitação chega, ela recebe um token, então ficamos com 9, mais 3 solicitações chegam e cada uma recebe um token, deixando-nos com 6 tokens. Depois de um minuto, recebemos 2 novos tokens, o que nos coloca em 8, as solicitações chegam e pegam os tokens restantes, deixando-nos com 0. Se outra solicitação chegar, não será permitido acessar o recurso até que ganhemos mais tokens, o que acontece a cada minuto. Após 5 minutos sem solicitações, o bucket terá todos os 10 tokens novamente e não adicionará mais nenhum nos minutos subsequentes, a menos que as solicitações obtenham mais tokens.

|Tempo  |Disponível| Obtido| Adicionado| Carregar|
|:----: |:--------:|:-----:|:---------:|:-------:|
|0      |100       |20     |0          |80       |
|10	    |80	       |10	   |20	       |90       |
|20	    |90	       |5	   |15	       |100      |
|30	    |100       |30	   |20	       |90       |
|40	    |90	       |6	   |16	       |100      |
|50	    |100       |40	   |20	       |80       |
|60	    |80	       |50	   |20	       |50       |

O código a seguir usa o limitador de bucket de token:

```csharp
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using WebRateLimitAuth.Models;

var builder = WebApplication.CreateBuilder(args);

var tokenPolicy = "token";
var myOptions = new MyRateLimitOptions();
builder.Configuration.GetSection(MyRateLimitOptions.MyRateLimit).Bind(myOptions);

builder.Services.AddRateLimiter(_ => _
    .AddTokenBucketLimiter(policyName: tokenPolicy, options =>
    {
        options.TokenLimit = myOptions.TokenLimit;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = myOptions.QueueLimit;
        options.ReplenishmentPeriod = TimeSpan.FromSeconds(myOptions.ReplenishmentPeriod);
        options.TokensPerPeriod = myOptions.TokensPerPeriod;
        options.AutoReplenishment = myOptions.AutoReplenishment;
    }));

var app = builder.Build();

app.UseRateLimiter();

static string GetTicks() => (DateTime.Now.Ticks & 0x11111).ToString("00000");

app.MapGet("/", () => Results.Ok($"Token Limiter {GetTicks()}"))
                           .RequireRateLimiting(tokenPolicy);

app.Run();
```

## Fixed window limit

O algoritmo de janela fixa usa o conceito de janela que também será usado no próximo algoritmo. A janela é um período de tempo que nosso limite é aplicado antes de passarmos para a próxima janela. No caso da janela fixa, passar para a próxima janela significa redefinir o limite de volta ao seu ponto inicial. Vamos imaginar que existe uma sala de cinema com sala única com capacidade para 100 pessoas e que a exibição do filme dura 2 horas. Quando o filme começa, deixamos as pessoas começarem a fazer fila para a próxima exibição, que será em 2 horas, até 100 pessoas podem fazer fila antes de começarmos a dizer-lhes para voltarem em outro momento. Assim que o filme de 2 horas terminar, a fila de 0 a 100 pessoas pode entrar na sala de cinema e reiniciamos a fila. Isto é o mesmo que mover a janela no algoritmo de janela fixa.

Considere o Seguinte Código:

```csharp
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4;
        options.Window = TimeSpan.FromSeconds(12);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));

var app = builder.Build();

app.UseRateLimiter();

static string GetTicks() => (DateTime.Now.Ticks & 0x11111).ToString("00000");

app.MapGet("/", () => Results.Ok($"Hello {GetTicks()}"))
                           .RequireRateLimiting("fixed");

app.Run();
```

O código anterior:
* Chama [AddRateLimiter](https://learn.microsoft.com/pt-br/dotnet/api/microsoft.aspnetcore.builder.ratelimiterservicecollectionextensions.addratelimiter?view=aspnetcore-8.0) para adicionar um serviço de limitação de taxa à coleção de serviços;
* Chama AddFixedWindowLimiter para criar um limitador de janela fixa com um nome de política "fixed" e define:
* [PermitLimit](https://learn.microsoft.com/pt-br/dotnet/api/system.threading.ratelimiting.fixedwindowratelimiteroptions.permitlimit?view=aspnetcore-8.0#system-threading-ratelimiting-fixedwindowratelimiteroptions-permitlimit) como 4 e o tempo [Window](https://learn.microsoft.com/pt-br/dotnet/api/system.threading.ratelimiting.fixedwindowratelimiteroptions.window?view=aspnetcore-8.0#system-threading-ratelimiting-fixedwindowratelimiteroptions-window) como 12. Um máximo de 4 solicitações por cada janela de 12 segundos são permitidas.
* [QueueProcessingOrder](https://learn.microsoft.com/pt-br/dotnet/api/system.threading.ratelimiting.queueprocessingorder?view=aspnetcore-8.0#system-threading-ratelimiting-queueprocessingorder-oldestfirst) para OldestFirst: Quando definido o parametro `QueueLimit` permiti obter as solicitações mais antingas da fila.
* [QueueLimit](https://learn.microsoft.com/pt-br/dotnet/api/system.threading.ratelimiting.fixedwindowratelimiteroptions.queuelimit?view=aspnetcore-8.0#system-threading-ratelimiting-fixedwindowratelimiteroptions-queuelimit) Difine que a fila de espera será de no máximo 2 solicitações, passando disso será emitido erro automaticamente.

## Sliding window limit

O algoritmo de janela deslizante é semelhante ao algoritmo de janela fixa, mas com a adição de segmentos `(O intervalo do segmento é (tempo da janela)/(segmentos por janela))`. Um segmento faz parte de uma janela, se pegarmos a janela anterior de 2 horas e dividi-la em 4 segmentos, teremos agora 4 segmentos de 30 minutos. Há também um índice de segmento atual que sempre apontará para o segmento mais recente em uma janela. As solicitações durante um período de 30 minutos vão para o segmento atual e a cada 30 minutos a janela desliza um segmento. Se houve alguma solicitação durante o segmento que a janela passou, ela será atualizada e nosso limite aumentará nessa quantidade. Se não houve nenhuma solicitação nosso limite permanece o mesmo.

Por exemplo, vamos usar o algoritmo de janela deslizante com 3 segmentos de 10 minutos e um limite de 100 solicitações. Nosso estado inicial é de 3 segmentos, todos com 0 contagens e nosso índice de segmento atual está apontando para o terceiro segmento.

![sliding_part1](./content/sliding_part1.png)

Durante os primeiros 10 minutos recebemos 50 solicitações, todas rastreadas no 3º segmento (nosso índice de segmento atual). Após os 10 minutos, deslizamos a janela em 1 segmento, movendo também nosso índice de segmento atual para o 4º segmento. Quaisquer solicitações usadas no primeiro segmento serão agora adicionadas de volta ao nosso limite. Como não houve, nosso limite é de 50 (pois 50 já são utilizados no 3º segmento).

![sliding_part2](./content/sliding_part2.png)

Durante os próximos 10 minutos recebemos mais 20 solicitações, então temos 50 no 3º segmento e 20 no 4º segmento agora. Novamente, deslizamos a janela após 10 minutos, de modo que nosso índice de segmento atual aponta para 5 e adicionamos quaisquer solicitações do segmento 2 ao nosso limite.

![sliding_part3](./content/sliding_part3.png)

10 minutos depois deslizamos a janela novamente, desta vez quando a janela desliza o índice do segmento atual está em 6 e o ​​segmento 3 (aquele com 50 solicitações) está agora fora da janela. Então recuperamos as 50 solicitações e as adicionamos ao nosso limite, que agora será de 80, pois ainda existem 20 em uso pelo segmento 4.

![sliding_part4](./content/sliding_part4.png)

O código a seguir usa o limitador de taxa de janela deslizante:
```csharp
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using WebRateLimitAuth.Models;

var builder = WebApplication.CreateBuilder(args);

var myOptions = new MyRateLimitOptions();
builder.Configuration.GetSection(MyRateLimitOptions.MyRateLimit).Bind(myOptions);
var slidingPolicy = "sliding";

builder.Services.AddRateLimiter(_ => _
    .AddSlidingWindowLimiter(policyName: slidingPolicy, options =>
    {
        options.PermitLimit = myOptions.PermitLimit;
        options.Window = TimeSpan.FromSeconds(myOptions.Window);
        options.SegmentsPerWindow = myOptions.SegmentsPerWindow;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = myOptions.QueueLimit;
    }));

var app = builder.Build();

app.UseRateLimiter();

static string GetTicks() => (DateTime.Now.Ticks & 0x11111).ToString("00000");

app.MapGet("/", () => Results.Ok($"Sliding Window Limiter {GetTicks()}"))
                           .RequireRateLimiting(slidingPolicy);

app.Run();
```

A única configuração difente aqui que vale a penas destacar é: 
* [SegmentsPerWindow:](https://learn.microsoft.com/pt-br/dotnet/api/system.threading.ratelimiting.slidingwindowratelimiteroptions.segmentsperwindow?view=aspnetcore-8.0) Como mencionado acima essa propriedade é a responsável por fazer a divisão da janela em seguimentos. Esse valor deve ser maior ou igual a 0 e é de preenchimento obrigatório quando se utiliza o algortimo de `SlidingWindowLimiter`.

# Configurar o que ocorrer quando chegar ocorrer limitações
Normalmento quando o limite de solicitações é atigindo é gerado um Status Code 503, porém esse não é o melhor comportamento para exemplificar que o limite de requests foram atingindos.

Por esse existe a possibilidade de mudarmos esse comportamento e personalizar a mensagem de retorno.

Muitos serviços no mercado estabeleceram-se que o retorno mais adequado é: `429 Too Many Requestscódigo`. Para alterar o código de status, você pode definir a `RejectionStatusCodeopção`:

Além disso, há um delegate chamado `OnRejected` que você pode definir para personalizar a resposta enviada quando a limitação de taxa é acionada para uma solicitação. É uma boa prática comunicar o que aconteceu e por que se aplica um limite de taxa. Portanto, em vez de retornar “apenas um código de status” por padrão, você pode retornar algumas informações mais significativas. O delegate `OnRejected` dá acesso ao contexto de limite de taxa atual, incluindo o arquivo HttpContext.

Aqui está um exemplo que define o código de statusCode como 429 retorna uma resposta significativa. A resposta menciona quando tentar novamente (se disponível nos metadados de limitação de taxa).

Considere o código abaixo:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            await context.HttpContext.Response.WriteAsync($"Limite de solicitações foi atingido. Tente novamente dentro de {retryAfter.TotalSeconds} segundos.");
        }
        else
        {
            await context.HttpContext.Response.WriteAsync("Limite de solicitações foi atingido. Tente novamente mais tarde.");
        }
    };
});
```
Dado que você tem acesso ao atual HttpContext, você também tem acesso à coleção de serviços. É uma boa prática ficar de olho em quem, quando e por que um limite de taxa está sendo aplicado, e você pode registrar isso obtendo um `ILoggerde, context.HttpContext.RequestServicesse` necessário.

> **Nota:** Tenha cuidado com a lógica que você escreve na sua implementação do `OnRejected`. Se você usar o contexto do seu  banco de dados e executar cinco consultas, seu limite de taxa não estará realmente ajudando a reduzir a pressão no seu banco de dados. Comunique-se com o usuário e retorne um erro significativo (você pode até usar o Acceptcabeçalho e retornar JSON ou HTML dependendo do tipo de cliente), mas não consuma mais recursos do que uma resposta normal exigiria.


# Create chained limiters
A API [CreateChained](https://learn.microsoft.com/pt-br/dotnet/api/system.threading.ratelimiting.partitionedratelimiter.createchained?view=aspnetcore-8.0) permite passar vários [PartitionedRateLimiter](https://learn.microsoft.com/pt-br/dotnet/api/system.threading.ratelimiting.partitionedratelimiter?view=aspnetcore-8.0) que são combinados em um `PartitionedRateLimiter`. O limitador combinado executa todos os limitadores de entrada em sequência.

O código a seguir usa `CreateChained`:
```csharp
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(_ =>
{
    _.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int) retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.");

        return new ValueTask();
    };
    _.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            return RateLimitPartition.GetFixedWindowLimiter
            (userAgent, _ =>
                new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 4,
                    Window = TimeSpan.FromSeconds(2)
                });
        }),
        PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            
            return RateLimitPartition.GetFixedWindowLimiter
            (userAgent, _ =>
                new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 20,    
                    Window = TimeSpan.FromSeconds(30)
                });
        }));
});

var app = builder.Build();
app.UseRateLimiter();

static string GetTicks() => (DateTime.Now.Ticks & 0x11111).ToString("00000");

app.MapGet("/", () => Results.Ok($"Hello {GetTicks()}"));

app.Run();
```
Se [GlobalLimiter](https://learn.microsoft.com/pt-br/dotnet/api/microsoft.aspnetcore.ratelimiting.ratelimiteroptions.globallimiter?view=aspnetcore-8.0#microsoft-aspnetcore-ratelimiting-ratelimiteroptions-globallimiter) for fornecido, ele será executado antes de qualquer política especificada em um terminal. Por exemplo, se você quiser limitar seu aplicativo para lidar com 1.000 solicitações simultâneas, independentemente de quais políticas de endpoint foram especificadas, você poderá configurar um PartitionedRateLimitercom essas configurações e definir a GlobalLimiterpropriedade.


# Vamos Começar

Para gerar token utilize a linha de comando dotnet [user-jwt](https://learn.microsoft.com/pt-br/aspnet/core/security/authentication/jwt-authn?view=aspnetcore-8.0&tabs=windows)

Comando para criar token padrão e adicioanr informações ao Appsettings.json
```bash
dotnet user-jwts create
```

A saída desse comando deve ser algo como demostrado abaixo:
>  New JWT saved with ID '4dce742'.  
>  Name: MyTestUser  
>  Scopes: myapi:secrets  
>  
>  Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6Ik15VGVzdFVzZXIiLCJzdWIiOiJNeVRlc3RVc2VyIiwianRpIjoiNGRjZTc0MiIsInNjb3BlIjoibXlhcGk6c2VjcmV0cyIsImF1ZCI6WyJodHRwOi8vbG9jYWxob3N0OjU2MzQwIiwiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzOTYiLCJodHRwOi8vbG9jYWxob3N0OjUyNjMiLCJodHRwczovL2xvY2FsaG9zdDo3MTA2Il0sIm5iZiI6MTcwNDMxNjA0MSwiZXhwIjoxNzEyMTc4NDQxLCJ2xvY2FsaG9zdDo3MTA2Il0sIm5iZiI6MTcwNDMxNjA0MSwiZXhwIjoxNzEyMTc4NDQxLCJpYXQiOjE3MDQzMTYwNDMsImlzcyI6ImRvdG5ldC11c2VyLWp3dHMifQ.Xh86IfOnss1GPbwiChdW72Su0KudZbkP6fSaxIy03DI


Para visualizar todas as informações do Token utilize o comando abaixo. Substituindo {id} por `JWT save ID` gerado quando usou o comando acima.
```bash
dotnet user-jwts print {ID} --show-all
```

Saída do comando acima:
> Found JWT with ID '4dce742'.  
> **ID:** 4dce742  
> **Name:** MyTestUser  
> **Scheme:** Bearer  
> **Audience(s):** http://localhost:56340, https://localhost:44396, http://localhost:5263, https://localhost:7106  
> **Not Before:** 2024-01-03T21:07:21.0000000+00:00  
> **Expires On:** 2024-04-03T21:07:21.0000000+00:00  
> **Scheme:** Bearer  
> **Audience(s):** http://localhost:56340, https://localhost:44396, http://localhost:5263, https://localhost:7106  
> **Not Before:** 2024-01-03T21:07:21.0000000+00:00  
> **Expires On:** 2024-04-03T21:07:21.0000000+00:00  
> **Issued On:** 2024-01-03T21:07:23.0000000+00:00  
> **Scopes:** myapi:secrets  
> **Roles:** [none]  
> **Custom Claims:** [none]  
> **Token Header:** {"alg":"HS256","typ":"JWT"}  
> **Token Payload:** {"unique_name":"MyTestUser","sub":"MyTestUser","jti":"4dce742","scope":"myapi:secrets","aud":["http://localhost:56340","https://localhost:44396","http://localhost:5263","https://localhost:7106"],"nbf":1704316041,"exp":1712178441,"iat":1704316043,"iss":"dotnet-user-jwts"}  
> **Compact Token:** eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6Ik15VGVzdFVzZXIiLCJzdWIiOiJNeVRlc3RVc2VyIiwianRpIjoiNGRjZTc0MiIsInNjb3BlIjoibXlhcGk6c2VjcmV0cyIsImF1ZCI6WyJodHRwOi8vbG9jYWxob3N0OjU2MzQwIiwiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzOTYiLCJodHRwOi8vbG9jYWxob3N0OjUyNjMiLCJodHRwczovL2xvY2FsaG9zdDo3MTA2Il0sIm5iZiI6MTcwNDMxNjA0MSwiZXhwIjoxNzEyMTc4NDQxLCJpYXQiOjE3MDQzMTYwNDMsImlzcyI6ImRvdG5ldC11c2VyLWp3dHMifQ.Xh86IfOnss1GPbwiChdW72Su0KudZbkP6fSaxIy03DI

Para começar a utilizar o sistem primeiro é necessário criar 2 tokens onde serão enviado nos testes. Para tal, iremos utilizar os comandos abaixo:
```bash
dotnet user-jwts create --name user1
dotnet user-jwts create --name user2
```

Os comandos acima devem ser executados no PATH do projeto WebAPI.

# Build e Test

### Entity Framework
Antes de executar os teste do projeto é necessário atualizar o banco de dados com as migrations que foram usadas no sistema. Certifique-se que o dotnet-ef esteja instalado e assim podemos executar a migration. Caso não esteja instalado utilize o comando abaixo.

```bash
dotnet tool install --global dotnet-ef
```

Agora vá até a pasta do projeto web e execute o comando abaixo para atualizar o banco de dados com as migrations.

```bash
dotnet ef database update
```
### Configurar servidor REDIS

Agora é necessário subir o Redis para podermos executar os este. Para isso execute o comando do docker abaixo para subir um container Redis no docker.

Esse comando primeiro vai limpar a tela do prompt depois, vai tentar remover todos os serviços que estão listados nos docker-compose em seguida tenta criar as imagens e subir os containers.

```bash
clear; docker compose down --remove-orphans --rmi local; docker compose up -d
```

Caso seja necessário executar o [redis-cli](https://redis.io/docs/connect/cli/) para analisar o conteúdo no [Redis](https://redis.io/), utilize o comando abaixo para conectar no docker do `redis-server` com o `redis-cli`.

O parametro `-a` dever ser informado a senha configurada no `docker-compose.yml`

```bash
docker exec -it redis-cache redis-cli -a eYVX7EwVmmxKPCDmwMtyKVge8oLd2t81
```
### Criar certificado com OpenSsl

Para instalar o OpenSsl e criar o certificado de conexão utilize o comando abaixo. Caso você não possua o [Chocolatey](https://chocolatey.org/install) instalado, siga a passo-a-passo da documentação;

```bash
choco install openssl -y
```
Agora é necessario criar o certificado para associar a conexão do redis. Para criar o certificado utilizado o comando abaixo do [dotnet dev-cert](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs)

```bash
dotnet dev-certs https -ep ./redis.crt -p redis-certificado-senha --trust --format PEM
```
O parâmetro `-p` informado no comando acima é a senha que deve ser usado ao utilizar o certificado, ou seja a senha ~~redis-certificado-senha~~ utilizada é apenas para ser utilizada em um ambiente de desenvolvimento.

Para converter o certificado do usuário e a chave privada do formato PEM para pfx, use este comando:

Esse comando somente pode ser utilizado depois que for executado o comando acima. É com ele que é criado os arquivos auto-assinado `redis.key` e `redis.crt`.

```bash
openssl pkcs12 -inkey redis.key -in redis.crt -export -out redis.pfx
```

### Executar Teste

Para rodar os teste do projeto, vá até a pasta do projeto de teste e execute o comando abaixo:

```bash
clear; dotnet test
```

# Referencia

- [Middleware Rate Limiting ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-8.0)
- [Maarten Balliauw - ASP.NET Core rate limiting middleware in .NET 7](https://blog.maartenballiauw.be/post/2022/09/26/aspnet-core-rate-limiting-middleware.html)
- [Brennan Conroy - Announcing Rate Limiting for .NET](https://devblogs.microsoft.com/dotnet/announcing-rate-limiting-for-dotnet/)
- [Manage JSON Web Tokens in development with dotnet user-jwts](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn?view=aspnetcore-8.0&tabs=windows)
- [Stefan Prodon - RateLimit com Redis baseado em IP ou ClientId](https://github.com/stefanprodan/AspNetCoreRateLimit)
- [RateLimit com Redis baseado na implementação Oficial da Microsoft](https://github.com/cristipufu/aspnetcore-redis-rate-limiting)
- [Rodando Redis no Docker](https://geshan.com.np/blog/2022/01/redis-docker/)