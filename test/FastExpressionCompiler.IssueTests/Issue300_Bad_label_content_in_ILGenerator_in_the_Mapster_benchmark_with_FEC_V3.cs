using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if LIGHT_EXPRESSION
using System.Text;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue300_Bad_label_content_in_ILGenerator_in_the_Mapster_benchmark_with_FEC_V3 : ITest
    {
        public int Run()
        {
            Test1();
            return 1;
        }

        [Test]
        public void Test1()
        {
            var p = new ParameterExpression[3]; // the parameter expressions 
            var e = new Expression[56]; // the unique expressions 
            var l = new LabelTarget[1]; // the labels 
            var expr = Lambda<Func<Customer, CustomerDTO, CustomerDTO>>( // $
            e[0]=Block(
                typeof(CustomerDTO),
                new[] {
                p[0]=Parameter(typeof(CustomerDTO), "result")
                },
                e[1]=Condition(
                e[2]=MakeBinary(ExpressionType.Equal,
                    p[1]=Parameter(typeof(Customer)),
                    e[3]=Constant(null, typeof(Customer))),
                e[4]=MakeGoto(System.Linq.Expressions.GotoExpressionKind.Return,
                    l[0]=Label(typeof(CustomerDTO)),
                    e[5]=Constant(null, typeof(CustomerDTO)),
                    typeof(void)),
                e[6]=Empty(),
                typeof(void)), 
                e[7]=MakeBinary(ExpressionType.Assign,
                p[0 // (CustomerDTO result)
                    ],
                e[8]=Coalesce(
                    p[2]=Parameter(typeof(CustomerDTO)),
                    e[9]=New( // 0 args
                    typeof(CustomerDTO).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))), 
                e[10]=Block(
                typeof(string),
                new ParameterExpression[0], 
                e[11]=MakeBinary(ExpressionType.Assign,
                    e[12]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Id")),
                    e[13]=Property(
                    p[1 // (Customer customer__62468121)
                        ],
                    typeof(Customer).GetTypeInfo().GetDeclaredProperty("Id"))), 
                e[14]=MakeBinary(ExpressionType.Assign,
                    e[15]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Name")),
                    e[16]=Property(
                    p[1 // (Customer customer__62468121)
                        ],
                    typeof(Customer).GetTypeInfo().GetDeclaredProperty("Name"))), 
                e[17]=MakeBinary(ExpressionType.Assign,
                    e[18]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Address")),
                    e[19]=Call(
                    e[20]=Call(
                        e[21]=Property(
                        null,
                        typeof(TypeAdapterConfig).GetTypeInfo().GetDeclaredProperty("GlobalSettings")), 
                        typeof(TypeAdapterConfig).GetMethods().Where(x => x.IsGenericMethod && x.Name == "GetMapToTargetFunction" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 2).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(Address), typeof(Address)) : x).Single()), 
                    typeof(System.Func<Address, Address, Address>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Invoke" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Address), typeof(Address) })),
                    e[22]=Property(
                        p[1 // (Customer customer__62468121)
                        ],
                        typeof(Customer).GetTypeInfo().GetDeclaredProperty("Address")), 
                    e[23]=Property(
                        p[0 // (CustomerDTO result)
                        ],
                        typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Address")))), 
                e[24]=MakeBinary(ExpressionType.Assign,
                    e[25]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("HomeAddress")),
                    e[26]=Call(
                    e[27]=Call(
                        e[28]=Property(
                        null,
                        typeof(TypeAdapterConfig).GetTypeInfo().GetDeclaredProperty("GlobalSettings")), 
                        typeof(TypeAdapterConfig).GetMethods().Where(x => x.IsGenericMethod && x.Name == "GetMapToTargetFunction" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 2).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(Address), typeof(AddressDTO)) : x).Single()), 
                    typeof(System.Func<Address, AddressDTO, AddressDTO>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Invoke" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Address), typeof(AddressDTO) })),
                    e[29]=Property(
                        p[1 // (Customer customer__62468121)
                        ],
                        typeof(Customer).GetTypeInfo().GetDeclaredProperty("HomeAddress")), 
                    e[30]=Property(
                        p[0 // (CustomerDTO result)
                        ],
                        typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("HomeAddress")))), 
                e[31]=MakeBinary(ExpressionType.Assign,
                    e[32]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Addresses")),
                    e[33]=Call(
                    e[34]=Call(
                        e[35]=Property(
                        null,
                        typeof(TypeAdapterConfig).GetTypeInfo().GetDeclaredProperty("GlobalSettings")), 
                        typeof(TypeAdapterConfig).GetMethods().Where(x => x.IsGenericMethod && x.Name == "GetMapToTargetFunction" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 2).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(Address[]), typeof(AddressDTO[])) : x).Single()), 
                    typeof(System.Func<Address[], AddressDTO[], AddressDTO[]>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Invoke" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Address[]), typeof(AddressDTO[]) })),
                    e[36]=Property(
                        p[1 // (Customer customer__62468121)
                        ],
                        typeof(Customer).GetTypeInfo().GetDeclaredProperty("Addresses")), 
                    e[37]=Property(
                        p[0 // (CustomerDTO result)
                        ],
                        typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Addresses")))), 
                e[38]=MakeBinary(ExpressionType.Assign,
                    e[39]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("WorkAddresses")),
                    e[40]=Call(
                    e[41]=Call(
                        e[42]=Property(
                        null,
                        typeof(TypeAdapterConfig).GetTypeInfo().GetDeclaredProperty("GlobalSettings")), 
                        typeof(TypeAdapterConfig).GetMethods().Where(x => x.IsGenericMethod && x.Name == "GetMapToTargetFunction" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 2).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(ICollection<Address>), typeof(List<AddressDTO>)) : x).Single()), 
                    typeof(System.Func<ICollection<Address>, List<AddressDTO>, List<AddressDTO>>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Invoke" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(ICollection<Address>), typeof(List<AddressDTO>) })),
                    e[43]=Property(
                        p[1 // (Customer customer__62468121)
                        ],
                        typeof(Customer).GetTypeInfo().GetDeclaredProperty("WorkAddresses")), 
                    e[44]=Property(
                        p[0 // (CustomerDTO result)
                        ],
                        typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("WorkAddresses")))), 
                e[45]=MakeBinary(ExpressionType.Assign,
                    e[46]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("AddressCity")),
                    e[47]=Condition(
                    e[48]=MakeBinary(ExpressionType.Equal,
                        e[49]=Property(
                        p[1 // (Customer customer__62468121)
                            ],
                        typeof(Customer).GetTypeInfo().GetDeclaredProperty("Address")),
                        e[50]=Constant(null, typeof(Address))),
                    e[51]=Constant(null, typeof(string)),
                    e[52]=Property(
                        e[49 // MemberAccess of Address
                        ],
                        typeof(Address).GetTypeInfo().GetDeclaredProperty("City")),
                    typeof(string)))), 
                e[53]=MakeGoto(System.Linq.Expressions.GotoExpressionKind.Return,
                l[0 // (customerdto__39451090)
                ],
                p[0 // (CustomerDTO result)
                    ],
                typeof(void)), 
                e[54]=Label(l[0 // (customerdto__39451090)
                ],
                e[55]=Constant(null, typeof(CustomerDTO)))),
            p[1 // (Customer customer__62468121)
                ], 
            p[2 // (CustomerDTO customerdto__25342185)
                ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var customer = new Customer();
            var customerDto = new CustomerDTO();

            var res = fs(customer, customerDto);

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            var res2 = ff(customer, customerDto);

            Assert.AreEqual(res, res2);
        }

        public class TypeAdapterConfig 
        {
            public static TypeAdapterConfig GlobalSettings { get; } = new TypeAdapterConfig();

            private readonly Dictionary<TypeTuple, Delegate> _mapToTargetDict = new Dictionary<TypeTuple, Delegate>();
            public Func<TSource, TDestination, TDestination> GetMapToTargetFunction<TSource, TDestination>()
            {
                return (TSource s, TDestination d) => d;
            }
        }

            public readonly struct TypeTuple : IEquatable<TypeTuple>
            {
                public bool Equals(TypeTuple other)
                {
                    return Source == other.Source && Destination == other.Destination;
                }

                public override bool Equals(object obj)
                {
                    if (!(obj is TypeTuple))
                        return false;
                    return Equals((TypeTuple)obj);
                }

                public override int GetHashCode()
                {
                    return (Source.GetHashCode() << 16) ^ (Destination.GetHashCode() & 65535);
                }

                public static bool operator ==(TypeTuple left, TypeTuple right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(TypeTuple left, TypeTuple right)
                {
                    return !left.Equals(right);
                }

                public Type Source { get; }
                public Type Destination { get; }

                public TypeTuple(Type source, Type destination)
                {
                    this.Source = source;
                    this.Destination = destination;
                }
            }

        public class Address
        {
            public int Id { get; set; }
            public string Street { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
        }

        public class AddressDTO
        {
            public int Id { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
        }

        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal? Credit { get; set; }
            public Address Address { get; set; }
            public Address HomeAddress { get; set; }
            public Address[] Addresses { get; set; }
            public ICollection<Address> WorkAddresses { get; set; }
        }

        public class CustomerDTO
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Address Address { get; set; }
            public AddressDTO HomeAddress { get; set; }
            public AddressDTO[] Addresses { get; set; }
            public List<AddressDTO> WorkAddresses { get; set; }
            public string AddressCity { get; set; }
        }
    }
}