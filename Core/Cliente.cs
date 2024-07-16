using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class Cliente
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public string? CPF { get; set; }

        public Cliente(Guid id, string nome, string email, string cpf)
        {
            Id = id;
            Nome = nome;
            Email = email;
            CPF = cpf;
        }

        public override string ToString()
        {
            return $"Cliente {Id}, Nome {Nome}, CPF {CPF}";
        }
    }
}
