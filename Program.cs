using System;
using System.Linq;
using Autofac;

namespace AutofacDecorators
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            
            builder.RegisterDecorated<CommandDispatcher, ICommandDispatcher>(
                    typeof(TransactionalCommandDispatcherDecorator),
                    typeof(LoggingCommandDispatcherDecorator)
                );

            builder.RegisterType<DependencyA>().AsSelf();

            var container = builder.Build();

            var dispatcher = container.Resolve<ICommandDispatcher>();

            var cmd = new DummyCommand();
            dispatcher.Execute(cmd);
        }
    }

    public static class BuilderExtensions
    {
        public static void RegisterDecorated<TBase, TInterface>(this ContainerBuilder builder, params Type[] decorators)
            where TBase : TInterface
        {
            builder.RegisterDecorated<TBase, TInterface>(typeof(TInterface).Name, decorators);
        }

        public static void RegisterDecorated<TBase, TInterface>(this ContainerBuilder builder, string keyBase, params Type[] decorators)
            where TBase : TInterface
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (!decorators.Any())
            {
                throw new ArgumentException("No decorators specified!");
            }

            if (decorators.Any(x => !x.IsAssignableTo<TInterface>()))
            {
                throw new ArgumentException("Decorator type is incompatible with expected interface!");
            }

            var numOfDecorators = decorators.Length;

            builder.RegisterType<TBase>().Named<TInterface>($"{keyBase}-0");

            for (int i = 1; i < numOfDecorators; i++)
            {
                var decorator = decorators[i - 1];

                var currentKey = $"{keyBase}-{i}";
                var previousKey = $"{keyBase}-{i - 1}";

                builder.RegisterType(decorator)
                    .WithParameter(
                        (parameterInfo, _) => parameterInfo.ParameterType == typeof(TInterface),
                        (_, context) => context.ResolveNamed<TInterface>(previousKey)
                    ).Named<TInterface>(currentKey);
            }

            builder.RegisterType(decorators.Last())
                .WithParameter(
                    (parameterInfo, _) => parameterInfo.ParameterType == typeof(TInterface),
                    (_, context) => context.ResolveNamed<TInterface>($"{keyBase}-{numOfDecorators - 1}")
                ).As<TInterface>();
        }
    }

    public interface ICommandDispatcher
    {
        void Execute<TCommand>(TCommand command)
            where TCommand : ICommand;
    }

    public class CommandDispatcher : ICommandDispatcher
    {
        public void Execute<TCommand>(TCommand command) where TCommand : ICommand
        {
            Console.WriteLine("Executing command!");
        }
    }

    public class TransactionalCommandDispatcherDecorator : ICommandDispatcher
    {
        private readonly ICommandDispatcher _dispatcher;
        private readonly DependencyA _dep;

        public TransactionalCommandDispatcherDecorator(ICommandDispatcher dispatcher, DependencyA dep)
        {
            _dispatcher = dispatcher;
            _dep = dep;
        }

        public void Execute<TCommand>(TCommand command)
            where TCommand : ICommand
        {
            Console.WriteLine("Transactional: Before");
            _dispatcher.Execute(command);
            Console.WriteLine("Transactional: After");
        }
    }

    public class LoggingCommandDispatcherDecorator : ICommandDispatcher
    {
        private readonly ICommandDispatcher _dispatcher;

        public LoggingCommandDispatcherDecorator(ICommandDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void Execute<TCommand>(TCommand command) where TCommand : ICommand
        {
            Console.WriteLine("Logging: Before");
            _dispatcher.Execute(command);
            Console.WriteLine("Logging: After");
        }
    }

    public class DependencyA { }

    public interface ICommand { }
    public class DummyCommand : ICommand { }
}
