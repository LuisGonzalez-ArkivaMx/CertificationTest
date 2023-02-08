using MFiles.VAF.Configuration;
using System.Runtime.Serialization;

namespace CertificationTest
{
    [DataContract]
    public class Configuration
    {
        [DataMember]
        [JsonConfEditor(TypeEditor = "options", Options = "{selectOptions:[\"Yes\",\"No\"]}", HelpText = "Enable or Disable the Application", Label = "Enabled", DefaultValue = "No")]
        public string Enabled { get; set; } = "No";

        [DataMember]
        [JsonConfEditor(HelpText = "Converting VB Script to a Vault Application", Label = "Converting VB Script to a Vault Application", IsRequired = true)]
        public ConvertingVBScriptToAVaultApplication ConvertingVBScriptToAVaultApplication { get; set; }

        [DataMember]
        [JsonConfEditor(HelpText = "User Groups by Role", Label = "User Groups by Role", IsRequired = true)]
        public UserGroupByRole UserGroupByRole { get; set; }

        [DataMember]
        [JsonConfEditor(HelpText = "Listing Valid and Expired Contracts", Label = "Listing Valid and Expired Contracts", IsRequired = true)]
        public ListingValidAndExpiredContracts ListingValidAndExpiredContracts { get; set; }

        [DataMember]
        [JsonConfEditor(HelpText = "Successor of a Contract Owner", Label = "Successor of a Contract Owner", IsRequired = true)]
        public SuccessorOfAContractOwner SuccessorOfAContractOwner { get; set; }
    }

    [DataContract]
    public class ConvertingVBScriptToAVaultApplication
    {
        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "Subject Property Definition", Label = "Subject Property", IsRequired = true)]
        public MFIdentifier PDSubject { get; set; }

        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "Customer Property Definition", Label = "Customer Property", IsRequired = true)]
        public MFIdentifier PDCustomer { get; set; }

        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "Supplier Property Definition", Label = "Supplier Property", IsRequired = true)]
        public MFIdentifier PDSupplier { get; set; }

        //[DataMember]
        //[MFPropertyDef]
        //[JsonConfEditor(HelpText = "Contract Title Property Definition", Label = "Contract Title Property", IsRequired = true)]
        //public MFIdentifier PDContractTitle { get; set; }
    }

    [DataContract]
    public class UserGroupByRole
    {
        [DataMember]
        [MFUserGroup]
        [JsonConfEditor(HelpText = "Contract Managers User Group", Label = "Contract Managers User Group", IsRequired = true)]
        public MFIdentifier UGContractManagers { get; set; }

        [DataMember]
        [MFUserGroup]
        [JsonConfEditor(HelpText = "Executive Management User Group", Label = "Executive Management User Group", IsRequired = true)]
        public MFIdentifier UGExecutiveManagement { get; set; }

        [DataMember]
        [MFClass]
        [JsonConfEditor(HelpText = "Person Class", Label = "Person Class", IsRequired = true)]
        public MFIdentifier CLPerson { get; set; }

        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "Roles Property Definition", Label = "Roles Property", IsRequired = true)]
        public MFIdentifier PDRoles { get; set; }

        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "M-Files User Property Definition", Label = "M-Files User Property", IsRequired = true)]
        public MFIdentifier PDMfilesUser { get; set; }
    }

    [DataContract]
    public class ListingValidAndExpiredContracts
    {
        [DataMember]
        [MFClass]
        [JsonConfEditor(HelpText = "Delivery Agreement Class", Label = "Delivery Agreement Class", IsRequired = true)]
        public MFIdentifier CLDeliveryAgreement { get; set; }

        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "Customer Property Definition", Label = "Customer Property", IsRequired = true)]
        public MFIdentifier PDCustomer { get; set; }

        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "Valid Contracts Property Definition", Label = "Valid Contracts Property", IsRequired = true)]
        public MFIdentifier PDValidContracts { get; set; }

        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "Expired Contracts Property Definition", Label = "Expired Contracts Property", IsRequired = true)]
        public MFIdentifier PDExpiredContracts { get; set; }

        [DataMember]
        [JsonConfEditor(HelpText = "Workflow Configuration", Label = "Workflow Configuration", IsRequired = true)]
        public WorkflowConfiguration WorkflowConfiguration { get; set; }
    }

    [DataContract]
    public class WorkflowConfiguration
    {
        [DataMember]
        [MFWorkflow]
        [JsonConfEditor(HelpText = "Contract Workflow", Label = "Contract Workflow", IsRequired = true)]        
        public MFIdentifier ContractWorkflow { get; set; }

        [DataMember]
        [MFState]        
        [JsonConfEditor(HelpText = "Signed Contract Workflow State", Label = "Signed Contract State", IsRequired = true)]
        public MFIdentifier SignedContractWorkflowState { get; set; }

        [DataMember]
        [MFState]
        [JsonConfEditor(HelpText = "Expired Contract Workflow State", Label = "Expired Contract State", IsRequired = true)]
        public MFIdentifier ExpiredContractWorkflowState { get; set; }
    }

    [DataContract]
    public class SuccessorOfAContractOwner
    {
        [DataMember]
        [MFClass]
        [JsonConfEditor(HelpText = "Person Class", Label = "Person Class", IsRequired = true)]
        public MFIdentifier CLPerson { get; set; }

        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "Successor Property Definition", Label = "Successor Property", IsRequired = true)]
        public MFIdentifier PDSuccessor { get; set; }

        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "Contract Owner Property Definition", Label = "Contract Owner Property", IsRequired = true)]
        public MFIdentifier PDContractOwner { get; set; }

        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "Document Property Definition", Label = "Document Property", IsRequired = true)]
        public MFIdentifier PDDocument { get; set; }

        [DataMember]
        [MFPropertyDef]
        [JsonConfEditor(HelpText = "Former Employee Property Definition", Label = "Former Employee Property", IsRequired = true)]
        public MFIdentifier PDFormerEmployee { get; set; }
    }
}
