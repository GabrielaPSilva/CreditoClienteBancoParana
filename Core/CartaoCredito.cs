using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class CartaoCredito
    {
        public Guid Id { get; set; }
        public decimal Limite { get; set; }
        public string NumCartao { get; set; }
        public string DataExp { get; set; }
        public bool Aprovado { get; set; }
        public Guid ClienteId { get; set; }

        public CartaoCredito(Guid id, decimal limite, bool aprovado, string numCartao, string dataExp, Guid clienteId)
        {
            Id = id;
            Limite = limite;
            Aprovado = aprovado;
            NumCartao = numCartao;
            DataExp = dataExp;
            ClienteId = clienteId;
        }
    }
}
