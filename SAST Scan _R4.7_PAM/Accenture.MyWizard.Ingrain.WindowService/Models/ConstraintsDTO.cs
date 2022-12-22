using System;
using System.Collections.Generic;
using System.Text;

namespace Accenture.MyWizard.Ingrain.WindowService.Models
{

    public class ConstructsDTO
    {
        public List<DeliveryConstructsDTO> DeliveryConstructs { get; set; }
    }

    public class DeliveryConstructsDTO
    {
        public string DeliveryConstructUId { get; set; }
        public string Name { get; set; }
        public string DeliveryConstructTypeUId { get; set; }
        public List<DeliveryConstructsDTO> DeliveryConstructs { get; set; }
    }


    public class IterationsArrayDTO
    {
        public IterationsArrayDTO()
        {
            Iterations = new List<IterationsDTO>();
        }

        public List<IterationsDTO> Iterations { get; set; }
        
    }

    public class IterationsDTO
    {
        public string IterationUId { get; set; }
        public string IterationId { get; set; }
        public string Name { get; set; }
        public string StartOn { get; set; }
        public string EndOn { get; set; }

    }

    public class ClosedUserStoryDTO
    {
        public int TotalRecordCount { get; set; }
        public int TotalPageCount { get; set; }
        public int CurrentPage { get; set; }
        public List<EntityDTO> Entity { get; set; }

    }

    public class EntityDTO
    { 
        public string stateuid { get; set; }
        public string workitemexternalid { get; set; }
        public string Iterationuid { get; set; }

    }

}
