using System;
using System.Collections.Generic;
using MFiles.VAF.AppTasks;
using MFiles.VAF.Common;
using MFiles.VAF.Core;
using MFilesAPI;
using MFiles.VAF.Common.ApplicationTaskQueue;
using Newtonsoft.Json;

namespace CertificationTest
{
    /// <summary>
    /// The entry point for this Vault Application Framework application.
    /// </summary>
    /// <remarks>Examples and further information available on the developer portal: http://developer.m-files.com/. </remarks>    

    public class VaultApplication
        : ConfigurableVaultApplicationBase<Configuration>
    {
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChanges, Class = "MF.CL.DeliveryAgreement")]
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChanges, Class = "MF.CL.SupplierAgreement")]
        public void ConvertingVBScriptToAVaultApplication(EventHandlerEnvironment env)
        {
            // Initialize variables and M-Files objects
            var oTypedValue = new TypedValue();
            var oPropVals = new PropertyValues();
            var documentTitle = "";

            // If the app is enabled, continue
            if (Configuration.Enabled.Equals("Yes"))
            {                
                try
                {
                    // Access to the values of the object's properties
                    oPropVals = env.Vault.ObjectPropertyOperations.GetProperties(env.ObjVer);

                    // Get the class name of the object valued
                    var className = oPropVals.SearchForProperty(100).TypedValue.DisplayValue;

                    // Get the subject property value
                    var subject = oPropVals
                        .SearchForProperty(Configuration.ConvertingVBScriptToAVaultApplication.PDSubject)
                        .TypedValue
                        .DisplayValue;                    

                    // If the class valued is Delivery Agreement
                    if (Convert.ToInt32(oPropVals.SearchForProperty(100).TypedValue.GetValueAsLookup().DisplayID) == 5)
                    {
                        // Get the customer property value
                        var customer = oPropVals
                        .SearchForProperty(Configuration.ConvertingVBScriptToAVaultApplication.PDCustomer)
                        .TypedValue
                        .DisplayValue;

                        // Calculate the value of the "Contract Title" property by concatenating the previously obtained properties
                        documentTitle = className + " - " + subject + " - " + customer;
                    }
                    // If the class valued is Supplier Agreement
                    else if (Convert.ToInt32(oPropVals.SearchForProperty(100).TypedValue.GetValueAsLookup().DisplayID) == 6)
                    {
                        // Get the supplier property value
                        var supplier = oPropVals
                        .SearchForProperty(Configuration.ConvertingVBScriptToAVaultApplication.PDSupplier)
                        .TypedValue
                        .DisplayValue;

                        // Calculate the value of the "Contract Title" property by concatenating the previously obtained properties
                        documentTitle = className + " - " + subject + " - " + supplier;
                    }

                    // Set the concatenation in the "Contract Title" property
                    oTypedValue.SetValue(MFDataType.MFDatatypeText, documentTitle);
                }
                catch (Exception ex)
                {
                    // Send the message error to event log
                    SysUtils.ReportErrorToEventLog("Error Message:  ", ex);
                }                
            }

            // Return the value
            return oTypedValue;
        }

        // Concurrent processing, allowing up to ten tasks to run at one time on each server.
        [TaskQueue(Behavior = MFTaskQueueProcessingBehavior.MFProcessingBehaviorConcurrent, MaxConcurrency = 10)]
        public const string ConcurrentTaskQueueId = "MFiles.Samples.ConcurrentTaskQueue.Application.TaskQueueId";
        public const string UserGroupByRoleTaskType = "UserGroupByRole";
        public const string SuccessorOfAContractOwnerTaskType = "SuccessorOfAContractOwner";

        [EventHandler(MFEventHandlerType.MFEventHandlerAfterCheckInChangesFinalize)]
        public void HandleEventHandlerAfterCheckInChangesFinalize(EventHandlerEnvironment env)
        {
            // Initialize M-Files object
            var oPropertyValues = new PropertyValues();

            // Access to the values of the object's properties
            oPropertyValues = env.Vault.ObjectPropertyOperations.GetProperties(env.ObjVer);

            // Validate that class is Person
            // This event should only be triggered for this class
            var pdClass = env.Vault
                .PropertyDefOperations
                .GetBuiltInPropertyDef(MFBuiltInPropertyDef.MFBuiltInPropertyDefClass);

            // Get the ID of the property definition of the class 
            var classID = oPropertyValues.SearchForPropertyEx(pdClass.ID, true).TypedValue.GetLookupID();

            if (classID == Configuration.UserGroupByRole.CLPerson.ID)
            {
                // When the object hits this state, add a task for it.
                this.TaskManager.AddTask
                (
                    env.Vault,
                    ConcurrentTaskQueueId,
                    UserGroupByRoleTaskType,
                    // Directives allow you to pass serializable data to and from the task.
                    directive: new ObjVerExTaskDirective(env.ObjVerEx)
                );

                // When the object hits this state, add a task for it.
                this.TaskManager.AddTask
                (
                    env.Vault,
                    ConcurrentTaskQueueId,
                    SuccessorOfAContractOwnerTaskType,
                    // Directives allow you to pass serializable data to and from the task.
                    directive: new ObjVerExTaskDirective(env.ObjVerEx)
                );
            }            
        }

        [TaskProcessor(ConcurrentTaskQueueId, UserGroupByRoleTaskType)]
        public void UserGroupByRole(ITaskProcessingJob<ObjVerExTaskDirective> job)
        {
            // Get the ObjVerEx of the object.
            if (false == job.Directive.TryGetObjVerEx(job.Vault, out ObjVerEx oObjVerEx))
                return;

            // Failing to call job.Commit will cause an exception.
            job.Commit((transactionalVault) =>
            {
                // Initialize M-Files object
                var oPropertyValues = new PropertyValues();

                // If the app is enabled, continue
                if (Configuration.Enabled.Equals("Yes"))
                {
                    try
                    {
                        // Access to the values of the object's properties
                        oPropertyValues = oObjVerEx.Properties;

                        // Validate that class is Person
                        // This event should only be triggered for this class
                        var pdClass = job.Vault
                            .PropertyDefOperations
                            .GetBuiltInPropertyDef(MFBuiltInPropertyDef.MFBuiltInPropertyDefClass);

                        // Get the ID of the property definition of the class 
                        var classID = oPropertyValues.SearchForPropertyEx(pdClass.ID, true).TypedValue.GetLookupID();

                        if (classID == Configuration.UserGroupByRole.CLPerson.ID)
                        {
                            // Validate the roles property definition exists and is not null or empty
                            if (oPropertyValues.IndexOf(Configuration.UserGroupByRole.PDRoles) != -1 &&
                                !oPropertyValues.SearchForPropertyEx(Configuration.UserGroupByRole.PDRoles, true).TypedValue.IsNULL())
                            {
                                // Get the roles property value
                                var roles = oPropertyValues
                                    .SearchForPropertyEx(Configuration.UserGroupByRole.PDRoles, true)
                                    .TypedValue
                                    .GetValueAsLookups();

                                // Get the M-Files User property value
                                var mfilesUserID = oPropertyValues
                                    .SearchForPropertyEx(Configuration.UserGroupByRole.PDMfilesUser, true)
                                    .TypedValue
                                    .GetLookupID();

                                // If the person has more than one role assigned, continue
                                if (roles.Count > 1)
                                {
                                    // Loop through each role found
                                    foreach (Lookup role in roles)
                                    {
                                        // Add the person to the "Contract Manager" group
                                        if (role.ItemGUID.Equals("F0D28476-F58D-440F-8E65-D3A58AA916C9"))
                                        {
                                            job.Vault.UserOperationsEx.AddMemberToUserGroup(
                                                Configuration.UserGroupByRole.UGContractManagers,
                                                mfilesUserID);                                            
                                        }

                                        // Add the person to the "Executive Management" group
                                        if (role.ItemGUID.Equals("9A9A3642-6E0F-4817-BFFF-4A7A14F7C000"))
                                        {
                                            job.Vault.UserOperationsEx.AddMemberToUserGroup(
                                                Configuration.UserGroupByRole.UGExecutiveManagement,
                                                mfilesUserID);
                                        }
                                    }
                                }
                                else if (roles.Count == 1) // If the person has one role assigned, continue
                                {
                                    // If the role is "Contract Manager"
                                    if (roles[1].ItemGUID.Equals("F0D28476-F58D-440F-8E65-D3A58AA916C9"))
                                    {
                                        // Add the person to the "Contract Manager" group
                                        job.Vault.UserOperationsEx.AddMemberToUserGroup(
                                            Configuration.UserGroupByRole.UGContractManagers,
                                            mfilesUserID);

                                        // Remove the person to the "Executive Management" group
                                        job.Vault.UserOperationsEx.RemoveMemberFromUserGroup(
                                            Configuration.UserGroupByRole.UGExecutiveManagement,
                                            mfilesUserID);
                                    }
                                    else // If the role is "Executive Management"
                                    {
                                        // Add the person to the "Executive Management" group
                                        job.Vault.UserOperationsEx.AddMemberToUserGroup(
                                            Configuration.UserGroupByRole.UGExecutiveManagement,
                                            mfilesUserID);

                                        // Remove the person to the "Contract Manager" group
                                        job.Vault.UserOperationsEx.RemoveMemberFromUserGroup(
                                            Configuration.UserGroupByRole.UGContractManagers,
                                            mfilesUserID);
                                    }
                                }
                                else // If the person has no roles assigned 
                                {
                                    // Remove the person to the "Executive Management" group
                                    job.Vault.UserOperationsEx.RemoveMemberFromUserGroup(
                                        Configuration.UserGroupByRole.UGExecutiveManagement,
                                        mfilesUserID);

                                    // Remove the person to the "Contract Manager" group
                                    job.Vault.UserOperationsEx.RemoveMemberFromUserGroup(
                                        Configuration.UserGroupByRole.UGContractManagers,
                                        mfilesUserID);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Send the message error to event log
                        SysUtils.ReportErrorToEventLog("Error Message:  ", ex);
                    }
                }
            });
        }

        [TaskProcessor(ConcurrentTaskQueueId, SuccessorOfAContractOwnerTaskType)]
        public void SuccessorOfAContractOwner(ITaskProcessingJob<ObjVerExTaskDirective> job)
        {
            // Get the ObjVerEx of the object.
            if (false == job.Directive.TryGetObjVerEx(job.Vault, out ObjVerEx oObjVerEx))
                return;

            // Failing to call job.Commit will cause an exception.
            job.Commit((transactionalVault) =>
            {
                // Initialize variables and M-Files objects
                var oPropertyValues = new PropertyValues();
                var oPropertyValue = new PropertyValue();

                // If the app is enabled, continue
                if (Configuration.Enabled.Equals("Yes"))
                {
                    // Access to the values of the object's properties
                    oPropertyValues = oObjVerEx.Properties;

                    // Validate that class is Person
                    // This event should only be triggered for this class
                    var pdClass = job.Vault
                        .PropertyDefOperations
                        .GetBuiltInPropertyDef(MFBuiltInPropertyDef.MFBuiltInPropertyDefClass);

                    // Get the ID of the property definition of the class 
                    var classID = oPropertyValues.SearchForPropertyEx(pdClass.ID, true).TypedValue.GetLookupID();

                    if (classID == Configuration.SuccessorOfAContractOwner.CLPerson.ID)
                    {
                        // If successor property exists and is not null or empty, continue
                        if (oPropertyValues.IndexOf(Configuration.SuccessorOfAContractOwner.PDSuccessor) != -1 &&
                            !oPropertyValues.SearchForPropertyEx(Configuration.SuccessorOfAContractOwner.PDSuccessor, true).TypedValue.IsNULL())
                        {
                            // Get the successor property value
                            var successor = oPropertyValues
                                .SearchForPropertyEx(Configuration.SuccessorOfAContractOwner.PDSuccessor, true)
                                .TypedValue
                                .GetValueAsLookup();

                            // Search all documents in which the person marked as former employee is a contract owner
                            var searchBuilder = new MFSearchBuilder(job.Vault);
                            searchBuilder.Deleted(false); // Don't search for deleted documents
                            searchBuilder.ObjType((int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument); // Filter by object of type document
                            searchBuilder.Property
                            (
                                Configuration.SuccessorOfAContractOwner.PDContractOwner,
                                MFDataType.MFDatatypeLookup,
                                oObjVerEx.ID //env.ObjVer.ID
                            ); // Search by contract owner property of the document

                            var searchResults = searchBuilder.FindEx();

                            // Through loop one by one all the documents found to assign them to the person marked as successor
                            foreach (var result in searchResults)
                            {
                                try
                                {
                                    // Add the successor in the "Contract Owner" property definition of the document
                                    var oObjVer = job.Vault.ObjectOperations.GetLatestObjVerEx(result.ObjID, true);
                                    oPropertyValue.PropertyDef = Configuration.SuccessorOfAContractOwner.PDContractOwner;
                                    oPropertyValue.TypedValue.SetValueToLookup(successor);
                                    oObjVer = job.Vault.ObjectOperations.CheckOut(result.ObjID).ObjVer;
                                    job.Vault.ObjectPropertyOperations.SetProperty(oObjVer, oPropertyValue);
                                    job.Vault.ObjectOperations.CheckIn(oObjVer);
                                }
                                catch (Exception ex)
                                {
                                    // If there is an error situation, create an assignment to the Successor 
                                    // with a link to the document where the error occurred
                                    CreateAssignment(result, successor);

                                    // Send the message error to event log
                                    SysUtils.ReportErrorToEventLog("Error Message:  ", ex);
                                }
                            }
                        }
                    }
                }
            });
        }

        // Sequential processing; all tasks will be executed one-by-one, in the order they were added to the queue.
        [TaskQueue(Behavior = MFTaskQueueProcessingBehavior.MFProcessingBehaviorSequential)]
        public const string SequentialTaskQueueId = "MFiles.Samples.SequentialTaskQueue.Application.TaskQueueId";
        public const string ListingValidAndExpiredContractsTaskType = "ListingValidAndExpiredContracts";

        [StateAction("MF.WFS.ContractWorkflow.SignedContractWorkflow")]
        [StateAction("MF.WFS.ContractWorkflow.ExpiredContractWorkflow")]
        public void HandleContractWorkflowState(StateEnvironment env)
        {
            // When the object hits this state, add a task for it.
            this.TaskManager.AddTask
            (
                env.Vault,
                SequentialTaskQueueId,
                ListingValidAndExpiredContractsTaskType,
                // Directives allow you to pass serializable data to and from the task.
                directive: new ObjVerExTaskDirective(env.ObjVerEx)
            );
        }

        [TaskProcessor(SequentialTaskQueueId, ListingValidAndExpiredContractsTaskType)]
        public void ListingValidAndExpiredContracts(ITaskProcessingJob<ObjVerExTaskDirective> job)
        {
            // Get the ObjVerEx of the object.
            if (false == job.Directive.TryGetObjVerEx(job.Vault, out ObjVerEx oObjVerEx))
                return;

            // Failing to call job.Commit will cause an exception.
            job.Commit((transactionalVault) =>
            {
                // Initialize M-Files object
                var oPropertyValues = new PropertyValues();

                // If the app is enabled, continue
                if (Configuration.Enabled.Equals("Yes"))
                {
                    try
                    {
                        // Access to the values of the object's properties
                        oPropertyValues = oObjVerEx.Properties;                   

                        // Validate that the class is Delivery Agreement
                        // This functionality should only be triggered for this class
                        var pdClass = job.Vault
                            .PropertyDefOperations
                            .GetBuiltInPropertyDef(MFBuiltInPropertyDef.MFBuiltInPropertyDefClass);

                        // Get the ID of the property definition of the class 
                        var classID = oPropertyValues.SearchForPropertyEx(pdClass.ID, true).TypedValue.GetLookupID();

                        if (classID == Configuration.ListingValidAndExpiredContracts.CLDeliveryAgreement.ID)
                        {
                            // Get the customer(s) ObjVerEx
                            var customers = oPropertyValues
                                .SearchForPropertyEx(Configuration.ListingValidAndExpiredContracts.PDCustomer, true)
                                .TypedValue
                                .GetValueAsLookups()
                                .ToObjVerExs(job.Vault);

                            // If found at least one customer in the Delivery Agreement
                            if (customers.Count > 0)
                            {
                                // Validate all documents in Signed status and add them to the customer's "Valid Contracts" property 
                                AddValidOrExpiredDocumentsInCustomer(
                                    customers,
                                    Configuration.ListingValidAndExpiredContracts.PDValidContracts,
                                    Configuration.ListingValidAndExpiredContracts.WorkflowConfiguration.SignedContractWorkflowState);

                                // Validate all documents in Signed status and add them to the customer's "Expired Contracts" property 
                                AddValidOrExpiredDocumentsInCustomer(
                                    customers,
                                    Configuration.ListingValidAndExpiredContracts.PDExpiredContracts,
                                    Configuration.ListingValidAndExpiredContracts.WorkflowConfiguration.ExpiredContractWorkflowState);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Send the message error to event log
                        SysUtils.ReportErrorToEventLog("Error Message:  ", ex);
                    }
                }
            });
        }        

        private void AddValidOrExpiredDocumentsInCustomer(List<ObjVerEx> customers, int contractsPropertyDef, int contractWorkflowState)
        {
            // Initialize variables and M-Files objects
            var oPropertyValue = new PropertyValue();
            var oLookups = new Lookups();
            var oLookup = new Lookup();

            // Loop through each customer found
            foreach (var customer in customers)
            {
                // Search Documents of the current customer and signed or expired workflow state
                var documents = GetDeliveryAgreementDocuments(customer, contractWorkflowState);

                // If at least one document was found
                if (documents.Count > 0)
                {
                    SysUtils.ReportInfoToEventLog("Documents: " + documents.Count);

                    // The found documents are added in the lookups object
                    foreach (var document in documents)
                    {
                        oLookup.Item = document.ID;
                        oLookups.Add(-1, oLookup);
                    }

                    // Add the documents en in the property definition (Valid Or Expired Contracts) of the customer
                    var oObjVer = customer.Vault.ObjectOperations.GetLatestObjVerEx(customer.ObjID, true);
                    oPropertyValue.PropertyDef = contractsPropertyDef;
                    oPropertyValue.TypedValue.SetValueToMultiSelectLookup(oLookups);
                    oObjVer = customer.Vault.ObjectOperations.CheckOut(customer.ObjID).ObjVer; // Check-out the object
                    customer.Vault.ObjectPropertyOperations.SetProperty(oObjVer, oPropertyValue);
                    customer.Vault.ObjectOperations.CheckIn(oObjVer); // Check-in the object
                }                
            }
        }  

        [PropertyValueValidation("MF.PD.ValidContracts")]
        public bool ValidContractsPropertyValidation(PropertyEnvironment env, out string message)
        {
            // Initialize bool variable
            bool bValidate = true;

            // If the app is enabled, continue
            if (Configuration.Enabled.Equals("Yes"))
            {
                try
                {
                    // If the customer has valid contracts, changes cannot be saved if the "Valid Contracts" property is empty
                    if (GetDeliveryAgreementDocuments(env.ObjVerEx, Configuration
                                                                        .ListingValidAndExpiredContracts
                                                                        .WorkflowConfiguration
                                                                        .SignedContractWorkflowState).Count > 0)
                    {
                        // Validate the property value
                        bValidate = env.PropertyValue?.Value?.DisplayValue != "";
                    }
                }
                catch (Exception ex)
                {
                    // Send the message error to event log
                    SysUtils.ReportErrorToEventLog("Error Message:  ", ex);
                }                
            }

            // Set the message (displayed if validation fails)
            message = "The property \"Valid Contracts\" cannot be empty, the customer has valid contracts.";

            // Return the validation result
            return bValidate;
        }

        [PropertyValueValidation("MF.PD.ExpiredContracts")]
        public bool ExpiredContractsPropertyValidation(PropertyEnvironment env, out string message)
        {
            // Initialize bool variable
            bool bValidate = true;

            // If the app is enabled, continue
            if (Configuration.Enabled.Equals("Yes"))
            {
                try
                {
                    // If the customer has expired contracts, changes cannot be saved if the "Expired Contracts" property is empty
                    if (GetDeliveryAgreementDocuments(env.ObjVerEx, Configuration
                                                                        .ListingValidAndExpiredContracts
                                                                        .WorkflowConfiguration
                                                                        .ExpiredContractWorkflowState).Count > 0)
                    {
                        // Validate the property value
                        bValidate = env.PropertyValue?.Value?.DisplayValue != "";
                    }
                }
                catch (Exception ex)
                {
                    // Send the message error to event log
                    SysUtils.ReportErrorToEventLog("Error Message:  ", ex);
                }
            }
            
            // Set the message (displayed if validation fails)
            message = "The property \"Expired Contracts\" cannot be empty, the customer has expired contracts.";

            // Return the validation result
            return bValidate;
        }

        private List<ObjVerEx> GetDeliveryAgreementDocuments(ObjVerEx customer, int contractWorkflowState)
        {
            // Initialize variables and M-Files objects
            var oSearchResults = new List<ObjVerEx>();
            var oLookups = new Lookups();
            var oLookup = new Lookup();

            // Add at Lookups the customer value
            oLookup.Item = customer.ID;
            oLookups.Add(-1, oLookup);

            // Search all delivery agreement documents in the status defined in the contractWorkflowState variable
            var searchBuilder = new MFSearchBuilder(customer.Vault);
            searchBuilder.Deleted(false); // Don't search for deleted documents
            searchBuilder.ObjType((int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument); // Filter by object of type document
            searchBuilder.Property
            (
                Configuration.ListingValidAndExpiredContracts.PDCustomer, 
                MFDataType.MFDatatypeMultiSelectLookup, 
                oLookups
            ); // Search in the Customer property definition
            searchBuilder.Property
            (
                (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClass,
                MFDataType.MFDatatypeLookup,
                Configuration.ListingValidAndExpiredContracts.CLDeliveryAgreement.ID
            ); // Filter search by Delivery Agreement class
            searchBuilder.Property
            (
                MFBuiltInPropertyDef.MFBuiltInPropertyDefState,
                MFDataType.MFDatatypeLookup,
                contractWorkflowState
            ); // Search only the documents that are in the status predefined in the contractWorkflowState variable

            // Load the results into a list of type ObjVerEx
            oSearchResults = searchBuilder.FindEx();

            // Return the value
            return oSearchResults;
        }

        [PropertyValueValidation("MF.PD.Successor")]
        public bool SuccessorPropertyValidation(PropertyEnvironment env, out string message)
        {
            // Initialize variables and M-Files objects
            bool bValidate = true;
            string sMessage = "";

            var oPropertyValues = new PropertyValues();

            // If the app is enabled, continue
            if (Configuration.Enabled.Equals("Yes"))
            {
                try
                {
                    // Access to the values of the object's properties
                    oPropertyValues = env.Vault.ObjectPropertyOperations.GetProperties(env.ObjVer);

                    // If successor property exists and is not null or empty, continue
                    if (oPropertyValues.IndexOf(Configuration.SuccessorOfAContractOwner.PDSuccessor) != -1 &&
                        !oPropertyValues.SearchForPropertyEx(Configuration.SuccessorOfAContractOwner.PDSuccessor, true).TypedValue.IsNULL())
                    {
                        // Get the successor property value
                        var successor = oPropertyValues
                            .SearchForPropertyEx(Configuration.SuccessorOfAContractOwner.PDSuccessor, true)
                            .TypedValue
                            .GetValueAsLookup();

                        // If a Person is marked as a former employee and no successor is named, show the next message
                        if (string.IsNullOrEmpty(successor.DisplayValue))
                        {
                            sMessage = "You must select a person in the \"Successor\" property to continue.";
                            bValidate = env.PropertyValue?.Value?.DisplayValue != "";
                        }

                        // If the Person marked as a former employee is set as his/her own Successor, show the next message in the vault
                        if (env.ObjVerEx.Title == successor.DisplayValue)
                        {
                            sMessage = "The successor must not be the same person as the former employee, please select another successor.";
                            bValidate = env.PropertyValue?.Value?.DisplayValue != env.ObjVerEx.Title;
                        }
                        else // Validate that the selected successor is not marked as a former employee
                        {
                            var oSuccesor = successor.ToObjVerEx(env.Vault);

                            // Access to the properties values of successor
                            oPropertyValues = env.Vault.ObjectPropertyOperations.GetProperties(oSuccesor.ObjVer);

                            var isFormerEmployee = (bool)oPropertyValues
                                .SearchForPropertyEx(Configuration.SuccessorOfAContractOwner.PDFormerEmployee, true)
                                .TypedValue
                                .Value;

                            if (isFormerEmployee == true)
                            {
                                sMessage = "The selected successor is marked as former employee, please select another person.";
                                bValidate = isFormerEmployee == false;
                            }
                        }                        
                    }
                }
                catch (Exception ex)
                {
                    // Send the message error to event log
                    SysUtils.ReportErrorToEventLog("Error Message:  ", ex);
                }
            }            

            // Set the message (displayed if validation fails).
            message = sMessage;

            // Validate.
            return bValidate;
        }        

        private void CreateAssignment(ObjVerEx document, Lookup successor)
        {
            // Initialize variables and M-Files objects
            var oLookupsAssignedTo = new Lookups();
            var oLookupsDocument = new Lookups();
            var oLookupDocument = new Lookup();

            // Add the successor in the lookups classes
            oLookupsAssignedTo.Add(-1, successor);

            oLookupDocument.Item = document.ID;
            oLookupsDocument.Add(-1, oLookupDocument);

            // Concatenate name or title
            var nameOrTitlePropVal = "Assignment: " + document.Title;

            // Add the properties and define the class to create
            var builder = new MFPropertyValuesBuilder(document.Vault);
            builder.SetClass(MFBuiltInObjectClass.MFBuiltInObjectClassGenericAssignment); // Assignment Class
            builder.Add
            (
                (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle,
                MFDataType.MFDatatypeText,
                nameOrTitlePropVal // Name or title
            );
            builder.Add
            (
                (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignedTo,
                MFDataType.MFDatatypeMultiSelectLookup,
                oLookupsAssignedTo // Assigned To
            );
            builder.Add
            (
                Configuration.SuccessorOfAContractOwner.PDDocument, 
                MFDataType.MFDatatypeMultiSelectLookup, 
                oLookupsDocument // Document
            );

            // Define the object type
            var assignmentObjectTypeId = (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment;

            // Create the assignment object
            var objectVersion = document.Vault.ObjectOperations.CreateNewObjectEx
            (
                assignmentObjectTypeId,
                builder.Values,
                CheckIn: true
            );
        }
    }
}