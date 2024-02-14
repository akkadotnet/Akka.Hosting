//-----------------------------------------------------------------------
// <copyright file="ReceiveTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------
namespace Akka.Hosting.TestKit.MsTest.Tests.TestKitBaseTests;

[TestClass]
public class ReceiveTests : TestKit
{
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [TestMethod]
    public void ReceiveN_should_receive_correct_number_of_messages()
    {
        TestActor.Tell("1");
        TestActor.Tell("2");
        TestActor.Tell("3");
        TestActor.Tell("4");
        ReceiveN(3).Should().BeEquivalentTo(new[] { "1", "2", "3" }, opt => opt.WithStrictOrdering());
        ReceiveN(1).Should().BeEquivalentTo(new[] { "4" });
    }

    [TestMethod]
    public void ReceiveN_should_timeout_if_no_messages()
    {
        Invoking(() => ReceiveN(3, TimeSpan.FromMilliseconds(10)))
            .Should().Throw<Exception>();
    }

    [TestMethod]
    public void ReceiveN_should_timeout_if_to_few_messages()
    {
        TestActor.Tell("1");
        TestActor.Tell("2");
        Invoking(() => ReceiveN(3, TimeSpan.FromMilliseconds(100)))
            .Should().Throw<Exception>();
    }


    [TestMethod]
    public void FishForMessage_should_return_matched_message()
    {
        TestActor.Tell(1);
        TestActor.Tell(2);
        TestActor.Tell(10);
        TestActor.Tell(20);
        FishForMessage<int>(i => i >= 10).Should().Be(10);
    }

    [TestMethod]
    public void FishForMessage_should_timeout_if_no_messages()
    {
        Invoking(() => FishForMessage(_ => false, TimeSpan.FromMilliseconds(10)))
            .Should().Throw<Exception>();
    }

    [TestMethod]
    public void FishForMessage_should_timeout_if_to_few_messages()
    {
        TestActor.Tell("1");
        TestActor.Tell("2");
        Invoking(() => FishForMessage(_ => false, TimeSpan.FromMilliseconds(100)))
            .Should().Throw<Exception>();
    }

    [TestMethod]
    public async Task FishForMessage_should_fill_the_all_messages_param_if_not_null()
    {
        await Task.Run(delegate
        {
            var probe = base.CreateTestProbe("probe");
            probe.Tell("1");
            probe.Tell(2);
            probe.Tell("3");
            probe.Tell(4);
            var allMessages = new ArrayList();
            probe.FishForMessage<string>(isMessage: s => s == "3", allMessages: allMessages);
            allMessages.Should().BeEquivalentTo(new ArrayList { "1", 2 });
        });
    }

    [TestMethod]
    public async Task FishForMessage_should_clear_the_all_messages_param_if_not_null_before_filling_it()
    {
        await Task.Run(delegate
        {
            var probe = base.CreateTestProbe("probe");
            probe.Tell("1");
            probe.Tell(2);
            probe.Tell("3");
            probe.Tell(4);
            var allMessages = new ArrayList() { "pre filled data" };
            probe.FishForMessage<string>(isMessage: x => x == "3", allMessages: allMessages);
            allMessages.Should().BeEquivalentTo(new ArrayList { "1", 2 });
        });
    }

    [TestMethod]
    public async Task FishUntilMessageAsync_should_succeed_with_good_input()
    {
        var probe = CreateTestProbe("probe");
        probe.Ref.Tell(1d, TestActor);
        await probe.FishUntilMessageAsync<int>(max: TimeSpan.FromMilliseconds(10));
    }


    [TestMethod]
    public async Task FishUntilMessageAsync_should_fail_with_bad_input()
    {
        var probe = CreateTestProbe("probe");
        probe.Ref.Tell(3, TestActor);
        Func<Task> func = () => probe.FishUntilMessageAsync<int>(max: TimeSpan.FromMilliseconds(10));
        await func.Should().ThrowAsync<Exception>();
    }

    [TestMethod]
    public async Task WaitForRadioSilenceAsync_should_succeed_immediately_with_null_good_input()
    {
        var probe = CreateTestProbe("probe");
        var messages = await probe.WaitForRadioSilenceAsync(max: TimeSpan.FromMilliseconds(0));
        messages.Should().BeEquivalentTo(new ArrayList());
    }

    [TestMethod]
    public async Task WaitForRadioSilenceAsync_should_succeed_immediately_with_good_pre_input()
    {
        var probe = CreateTestProbe("probe");
        probe.Ref.Tell(1, TestActor);
        var messages = await probe.WaitForRadioSilenceAsync(max: TimeSpan.FromMilliseconds(0));
        messages.Should().BeEquivalentTo(new ArrayList { 1 });
    }

    [TestMethod]
    public async Task WaitForRadioSilenceAsync_should_succeed_later_with_good_post_input()
    {
        var probe = CreateTestProbe("probe");
        var task = probe.WaitForRadioSilenceAsync();
        probe.Ref.Tell(1, TestActor);
        var messages = await task;
        messages.Should().BeEquivalentTo(new ArrayList { 1 });
    }

    [TestMethod]
    public async Task WaitForRadioSilenceAsync_should_reset_timer_twice_only()
    {
        var probe = CreateTestProbe("probe");
        var max = TimeSpan.FromMilliseconds(3000);
        var halfMax = TimeSpan.FromMilliseconds(max.TotalMilliseconds / 2);
        var doubleMax = TimeSpan.FromMilliseconds(max.TotalMilliseconds * 2);
        var task = probe.WaitForRadioSilenceAsync(max: max, maxMessages: 2);
        await Task.Delay(halfMax);
        probe.Ref.Tell(1, TestActor);
        await Task.Delay(halfMax);
        probe.Ref.Tell(2, TestActor);
        await Task.Delay(doubleMax);
        probe.Ref.Tell(3, TestActor);
        var messages = await task;
        messages.Should().BeEquivalentTo(new ArrayList { 1, 2 });
    }

    [TestMethod]
    public async Task WaitForRadioSilenceAsync_should_fail_immediately_with_bad_input()
    {
        var probe = CreateTestProbe("probe");
        probe.Ref.Tell(3, TestActor);
        try
        {
            await probe.WaitForRadioSilenceAsync(max: TimeSpan.FromMilliseconds(0), maxMessages: 0);
            Assert.IsTrue(false, "we should never get here");
        }
        catch (AssertFailedException) { }
    }

    [TestMethod]
    public void ReceiveWhile_Filter_should_on_a_timeout_return_no_messages()
    {
        ReceiveWhile<object>(_ => _, TimeSpan.FromMilliseconds(10)).Count.Should().Be(0);
    }

    [TestMethod]
    public void ReceiveWhile_Filter_should_break_on_function_returning_null_and_return_correct_messages()
    {
        TestActor.Tell("1");
        TestActor.Tell(2);
        TestActor.Tell("3");
        TestActor.Tell(99999.0);
        TestActor.Tell(4);
        ReceiveWhile<string>(_ => (_ is double ? null : _.ToString())!)
            .Should().BeEquivalentTo(new[] { "1", "2", "3" }, opt => opt.WithStrictOrdering());
    }

    [TestMethod]
    public void ReceiveWhile_Filter_should_not_consume_last_message_that_didnt_match()
    {
        TestActor.Tell("1");
        TestActor.Tell("2");
        TestActor.Tell(4711);
        ReceiveWhile<object>(_ => (_ is string ? _ : null)!);
        ExpectMsg(4711);
    }

    [TestMethod]
    public void ReceiveWhile_Predicate_should_on_a_timeout_return_no_messages()
    {
        ReceiveWhile<object>(_ => false, TimeSpan.FromMilliseconds(10)).Count.Should().Be(0);
    }

    [TestMethod]
    public void ReceiveWhile_Predicate_should_break_when_predicate_returns_false_and_return_correct_messages()
    {
        TestActor.Tell("1");
        TestActor.Tell("2");
        TestActor.Tell("3");
        TestActor.Tell("-----------");
        TestActor.Tell("4");
        ReceiveWhile<string>(s => s.Length == 1)
            .Should().BeEquivalentTo(new[] { "1", "2", "3" }, opt => opt.WithStrictOrdering());
    }

    [TestMethod]
    public void
        ReceiveWhile_Predicate_should_break_when_type_is_wrong_and_we_dont_ignore_those_and_return_correct_messages()
    {
        TestActor.Tell("1");
        TestActor.Tell("2");
        TestActor.Tell("3");
        TestActor.Tell(4);
        TestActor.Tell("5");
        ReceiveWhile<string>(s => s.Length == 1, shouldIgnoreOtherMessageTypes: false)
            .Should().BeEquivalentTo(new[] { "1", "2", "3" }, opt => opt.WithStrictOrdering());
    }

    [TestMethod]
    public void
        ReceiveWhile_Predicate_should_continue_when_type_is_other_but_we_ignore_other_types_and_return_correct_messages()
    {
        TestActor.Tell("1");
        TestActor.Tell("2");
        TestActor.Tell("3");
        TestActor.Tell(4);
        TestActor.Tell("5");
        ReceiveWhile<string>(s => s.Length == 1, shouldIgnoreOtherMessageTypes: true)
            .Should().BeEquivalentTo(new[] { "1", "2", "3", "5" }, opt => opt.WithStrictOrdering());
    }

    [TestMethod]
    public void ReceiveWhile_Predicate_should_not_consume_last_message_that_didnt_match()
    {
        TestActor.Tell("1");
        TestActor.Tell("2");
        TestActor.Tell(4711);
        TestActor.Tell("3");
        TestActor.Tell("4");
        TestActor.Tell("5");
        TestActor.Tell(6);
        TestActor.Tell("7");
        TestActor.Tell("8");

        var received = ReceiveWhile<object>(_ => _ is string);
        received.Should().BeEquivalentTo(new[] { "1", "2" }, opt => opt.WithStrictOrdering());
        
        ExpectMsg(4711);

        received = ReceiveWhile<object>(_ => _ is string);
        received.Should().BeEquivalentTo(new[] { "3", "4", "5" }, opt => opt.WithStrictOrdering());

        ExpectMsg(6);
    }
}