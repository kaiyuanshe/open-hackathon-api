namespace Kaiyuanshe.OpenHackathon.ServerTests.DependencyInjection
{
    #region RegisterSubTypes

    public interface ITestA
    {
    }

    public abstract class TestABase : ITestA
    { }

    public class TestASubA : TestABase { }
    public class TestASubB : TestABase { }

    #endregion

    #region RegisterSubTypesAsDirectInterfaces
    public interface ITestB { }

    public abstract class TestBBase : ITestB { };

    // multiple interfaces
    public interface ITestBSubA1 { }
    public interface ITestBSubA2 { }
    public class TestBSubA : TestBBase, ITestBSubA1, ITestBSubA2 { }

    // single
    public interface ITestBSubB { }
    public class TestBSubB : TestBBase, ITestBSubB { }

    // no direct interface, won't be registered
    public class TestBSubC : TestBBase { }
    #endregion

    #region RegisterSubTypesAsDirectInterfaces - Generic
    public abstract class TestEntityBase { }
    public interface ITestC<T> where T : TestEntityBase { }
    public abstract class TestCBase<T> : ITestC<T> where T : TestEntityBase { }

    // no direct interface, won't be registered
    public class TestEntityA : TestEntityBase { }
    public class TestCSubA : TestCBase<TestEntityA> { }

    // single interface
    public class TestEntityB : TestEntityBase { }
    public interface ITestCSubB : ITestC<TestEntityB> { }
    public class TestCSubB : TestCBase<TestEntityA>, ITestCSubB { }

    // multiple interfaces
    public class TestEntityC : TestEntityBase { }
    public interface ITestCSubC1 : ITestC<TestEntityB> { }
    public interface ITestCSubC2 { }
    public class TestCSubC : TestCBase<TestEntityC>, ITestCSubC1, ITestCSubC2 { }

    // sub-sub
    public class TestEntityD : TestEntityBase { }
    public interface ITestCSubD : ITestC<TestEntityD> { }
    public abstract class TestCSubD : TestCBase<TestEntityD> { }
    public class TestCSubDSub : TestCSubD, ITestCSubD { }

    // generic sub
    public abstract class WLog { }
    public class WLogSub : WLog { }
    public class WLogEntity<T> : TestEntityBase where T : WLog { }
    public interface IWLogTable<T> : ITestC<WLogEntity<T>> where T : WLog { }
    public interface IWLogSubTable : IWLogTable<WLogSub> { };
    public abstract class WLogTableBase<T> : TestCBase<WLogEntity<T>> where T : WLog { }
    public class WlogSubTable : WLogTableBase<WLogSub>, IWLogSubTable { }
    #endregion
}
