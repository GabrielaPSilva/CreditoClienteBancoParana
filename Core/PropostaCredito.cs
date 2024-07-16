using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class PropostaCredito
    {
        public Guid Id { get; set; }
        public decimal Limite { get; set; }
        public bool Aprovado { get; set; }
        public Guid ClienteId { get; set; }

        public PropostaCredito(Guid id, decimal limite, bool aprovado, Guid clienteId)
        {
            Id = id;
            Limite = limite;
            Aprovado = aprovado;
            ClienteId = clienteId;
        }
    }
}
