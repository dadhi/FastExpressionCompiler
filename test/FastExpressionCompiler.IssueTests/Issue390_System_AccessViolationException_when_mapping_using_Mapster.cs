#if NET7_0_OR_GREATER && !LIGHT_EXPRESSION
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Mapster;
using Mapster.Utils;
using MapsterMapper;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests;

[TestFixture]
public class Issue390_System_AccessViolationException_when_mapping_using_Mapster : ITest
{
    public int Run()
    {
        Test_mapping();

        return 1;
    }

    [Test]
    public void Test_mapping()
    {
        var auth = new AuthResultDto() { RefreshToken = new() };

        var token = DataMapper.Current.Map<Token>(auth);

        Assert.AreEqual(auth.RefreshToken.ExpirationDate.LocalDateTime, token.RefreshTokenExpirationDate);
    }

    public class DataMapper
    {
        private readonly Lazy<Mapper> _lazyMapper;
        public Mapper Mapper => _lazyMapper.Value;
        private static DataMapper _instance;
        private static TypeAdapterConfig _cfg;

        public static DataMapper Current
        {
            get
            {
                _instance ??= new DataMapper();
                return _instance;
            }
        }

        public DataMapper() => _lazyMapper = new Lazy<Mapper>(() => new Mapper(_cfg ??= Config()));

        public TSource Clone<TSource>(TSource source) => Mapper.Map<TSource, TSource>(source);

        public TDes Map<TSource, TDes>(TSource source) => Mapper.Map<TSource, TDes>(source);

        public TDes Map<TDes>(object source) => Mapper.Map<TDes>(source);

        private static TypeAdapterConfig Config()
        {
            var cfg = TypeAdapterConfig.GlobalSettings;
            cfg.Compiler = static e =>
            {
                try
                {
                    e.PrintCSharp();

                    var @cs = (Func<AuthResultDto, Token>)((AuthResultDto issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015) =>
                    {
                        MapContextScope scope = null;
                        if (issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015 == (AuthResultDto)null)
                        {
                            return (Token)null;
                        }
                        
                        scope = new MapContextScope();
                        try
                        {
                            object cache = null;
                            Dictionary<ReferenceTuple, object> references = null;
                            ReferenceTuple key = default;
                            Token result = null;
                            references = scope.Context.References;
                            key = new ReferenceTuple(
                                issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015,
                                typeof(Token));
                            if (references.TryGetValue(
                                key,
                                out cache))
                            {
                                return ((Token)cache);
                            }
                            
                            result = new Token();
                            references[key] = ((object)result);
                            result.RefreshTokenExpirationDate = ((Func<DateTime?, DateTime>)((DateTime? datetime__9799115) =>
                                    (datetime__9799115 == (DateTime?)null) ?
                                        DateTime.Parse("1/1/0001 12:00:00 AM") :
                                        ((DateTime)datetime__9799115))).Invoke(
                                (issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015.RefreshToken == (RefreshTokenDto)null) ?
                                    (DateTime?)null :
                                    ((DateTime?)issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015.RefreshToken.ExpirationDate.LocalDateTime));
                            return result;
                        }
                        finally
                        {
                            scope.Dispose();
                        }
                        
                        issue390_system_accessviolationexception_when_mapping_using_mapster_token__41962596:;
                    });

                    var fs = e.CompileSys();
                    fs.PrintIL();

                    // var ff = e.CompileFast();
                    var ff = e.CompileFast(true, flags: CompilerFlags.NoInvocationLambdaInlining);
                    Assert.IsNotNull(ff);
                    ff.PrintIL();

                    return ff;
                }
                catch (Exception)
                {
                    throw;
                }
            };

            cfg.RequireDestinationMemberSource = true;
            cfg.Default.PreserveReference(true);
            RegisterMappings(cfg);

            try
            {
                cfg.Compile();
            }
            catch (Exception)
            {
            }

            return cfg;
        }

        private static void RegisterMappings(TypeAdapterConfig cfg)
        {
            cfg.NewConfig<AuthResultDto, Token>().Map(
                static dst => dst.RefreshTokenExpirationDate,
                static src => src.RefreshToken.ExpirationDate.LocalDateTime);
        }
    }

    public class AuthResultDto
    {
        public RefreshTokenDto RefreshToken { get; set; }
    }

    public class RefreshTokenDto
    {
        public DateTimeOffset ExpirationDate { get; set; }
    }

    public class Token
    {
        public DateTime RefreshTokenExpirationDate { get; set; }
    }

}
#endif
