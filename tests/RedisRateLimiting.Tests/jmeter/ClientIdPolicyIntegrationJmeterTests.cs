using Abstracta.JmeterDsl;

using FluentAssertions;

namespace RedisRateLimiting.Tests.jmeter;

public class ClientIdPolicyIntegrationJmeterTests
{
    [Fact]
    public void Thread_Interacoes_Fixa()
    {
        var threadGroup = JmeterDsl.ThreadGroup(name: "teste", 
                                                threads: 15, 
                                                iterations: 10, 
                                                JmeterDsl.DummySampler("{\"status\" : \"OK\"}"), 
                                                JmeterDsl.HttpSampler("http://localhost:5263/Clients2")
                                                         .Header("client3", "3"));

        //var result = JmeterDsl.TestPlan(threadGroup, JmeterDsl.ResultsTreeVisualizer()).Run();
        var result = JmeterDsl.TestPlan(threadGroup).Run();

        result.Overall.ErrorsCount.Should().Be(0);
        result.Overall.SampleTimePercentile99.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Thread_Ineracao_Por_Tempo()
    {
        
        //arrange
        var durations = TimeSpan.FromSeconds(5);
        var threadGroup = JmeterDsl.ThreadGroup(name: "teste",
                                                threads: 15,
                                                duration: durations,
                                                JmeterDsl.DummySampler("{\"status\" : \"OK\"}"),
                                                JmeterDsl.HttpSampler("http://localhost:5263/Clients2")
                                                         .Header("client3", "3"));

        //act
        //var result = JmeterDsl.TestPlan(threadGroup, JmeterDsl.ResultsTreeVisualizer()).Run();
        var result = JmeterDsl.TestPlan(threadGroup).Run();


        //assert
        result.Overall.ErrorsCount.Should().Be(0);
        result.Overall.SampleTimePercentile99.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Test_Com_Show_Gui()
    {
        JmeterDsl.TestPlan(
            JmeterDsl.ThreadGroup(2, 10,
                JmeterDsl.HttpSampler("http://localhost:5263/Clients2"), 
                JmeterDsl.ResultsTreeVisualizer()
            )
        ).ShowInGui();
    }


    [Fact]
    public void Test_Com_Rampa()
    {
        //https://abstracta.github.io/jmeter-dotnet-dsl/guide/#thread-ramps-and-holds
        JmeterDsl.TestPlan(
            JmeterDsl.ThreadGroup()
            .RampToAndHold(10, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20))
            .RampToAndHold(100, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30))
            .RampTo(200, TimeSpan.FromSeconds(10))
            .RampToAndHold(100, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30))
            .RampTo(0, TimeSpan.FromSeconds(5))
            .Children(JmeterDsl.HttpSampler("http://localhost:5263/Clients2"), JmeterDsl.ResultsTreeVisualizer())
        ).ShowInGui();
    }

    [Fact]
    public void Test_Com_2_Groups()
    {
        JmeterDsl.TestPlan(
                JmeterDsl.ThreadGroup().RampTo(10, TimeSpan.FromSeconds(5)).HoldIterating(20),                    //rampa para 10 threads por 5 segundos(1 thread a cada meio segundo) e iteração de cada thread 20 vezes
                JmeterDsl.ThreadGroup().RampToAndHold(10, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20)       //semelhante ao acima, mas após aumentar a execução, mantendo a execução por 20 segundos
                )      
            .Children(JmeterDsl.HttpSampler("http://localhost:5263/Clients2"), JmeterDsl.ResultsTreeVisualizer()))
            .ShowInGui();
    }
}
