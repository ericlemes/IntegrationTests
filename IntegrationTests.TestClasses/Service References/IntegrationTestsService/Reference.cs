﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IntegrationTests.TestClasses.IntegrationTestsService {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="IntegrationTestsService.IIntegrationTestsService")]
    public interface IIntegrationTestsService {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IIntegrationTestsService/GetServiceTable", ReplyAction="http://tempuri.org/IIntegrationTestsService/GetServiceTableResponse")]
        IntegrationTests.ServiceClasses.Domain.ServiceTable GetServiceTable(int ServiceTableID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IIntegrationTestsService/GetServiceTable", ReplyAction="http://tempuri.org/IIntegrationTestsService/GetServiceTableResponse")]
        System.Threading.Tasks.Task<IntegrationTests.ServiceClasses.Domain.ServiceTable> GetServiceTableAsync(int ServiceTableID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IIntegrationTestsService/GetServiceTables", ReplyAction="http://tempuri.org/IIntegrationTestsService/GetServiceTablesResponse")]
        IntegrationTests.ServiceClasses.Domain.ServiceTable[] GetServiceTables(int IDInicial, int IDFinal);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IIntegrationTestsService/GetServiceTables", ReplyAction="http://tempuri.org/IIntegrationTestsService/GetServiceTablesResponse")]
        System.Threading.Tasks.Task<IntegrationTests.ServiceClasses.Domain.ServiceTable[]> GetServiceTablesAsync(int IDInicial, int IDFinal);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IIntegrationTestsService/GetServiceTablesAsynchronous", ReplyAction="http://tempuri.org/IIntegrationTestsService/GetServiceTablesAsynchronousResponse")]
        IntegrationTests.ServiceClasses.Domain.ServiceTable[] GetServiceTablesAsynchronous(int IDInicial, int IDFinal);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IIntegrationTestsService/GetServiceTablesAsynchronous", ReplyAction="http://tempuri.org/IIntegrationTestsService/GetServiceTablesAsynchronousResponse")]
        System.Threading.Tasks.Task<IntegrationTests.ServiceClasses.Domain.ServiceTable[]> GetServiceTablesAsynchronousAsync(int IDInicial, int IDFinal);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IIntegrationTestsServiceChannel : IntegrationTests.TestClasses.IntegrationTestsService.IIntegrationTestsService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class IntegrationTestsServiceClient : System.ServiceModel.ClientBase<IntegrationTests.TestClasses.IntegrationTestsService.IIntegrationTestsService>, IntegrationTests.TestClasses.IntegrationTestsService.IIntegrationTestsService {
        
        public IntegrationTestsServiceClient() {
        }
        
        public IntegrationTestsServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public IntegrationTestsServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public IntegrationTestsServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public IntegrationTestsServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public IntegrationTests.ServiceClasses.Domain.ServiceTable GetServiceTable(int ServiceTableID) {
            return base.Channel.GetServiceTable(ServiceTableID);
        }
        
        public System.Threading.Tasks.Task<IntegrationTests.ServiceClasses.Domain.ServiceTable> GetServiceTableAsync(int ServiceTableID) {
            return base.Channel.GetServiceTableAsync(ServiceTableID);
        }
        
        public IntegrationTests.ServiceClasses.Domain.ServiceTable[] GetServiceTables(int IDInicial, int IDFinal) {
            return base.Channel.GetServiceTables(IDInicial, IDFinal);
        }
        
        public System.Threading.Tasks.Task<IntegrationTests.ServiceClasses.Domain.ServiceTable[]> GetServiceTablesAsync(int IDInicial, int IDFinal) {
            return base.Channel.GetServiceTablesAsync(IDInicial, IDFinal);
        }
        
        public IntegrationTests.ServiceClasses.Domain.ServiceTable[] GetServiceTablesAsynchronous(int IDInicial, int IDFinal) {
            return base.Channel.GetServiceTablesAsynchronous(IDInicial, IDFinal);
        }
        
        public System.Threading.Tasks.Task<IntegrationTests.ServiceClasses.Domain.ServiceTable[]> GetServiceTablesAsynchronousAsync(int IDInicial, int IDFinal) {
            return base.Channel.GetServiceTablesAsynchronousAsync(IDInicial, IDFinal);
        }
    }
}
