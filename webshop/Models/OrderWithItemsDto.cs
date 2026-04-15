using System.Collections.Generic;

namespace webshop.Models;

public class OrderWithItemsDto
{
    public Rendelesek? Order { get; set; }
    public List<RendelesTetelek>? Tetelek { get; set; }
}
