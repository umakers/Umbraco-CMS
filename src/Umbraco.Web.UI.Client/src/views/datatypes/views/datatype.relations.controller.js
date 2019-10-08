/**
 * @ngdoc controller
 * @name Umbraco.Editors.DataType.RelationsController
 * @function
 *
 * @description
 * The controller for the relations view of the datatype editor
 */
function DataTypeRelationsController($scope, $routeParams, dataTypeResource) {
    
    var vm = this;

    vm.relations = {};
    vm.hasRelations = false;
    
    vm.view = {};
    vm.view.loading = true;

    //we are editing so get the content item from the server
    dataTypeResource.getRelations($routeParams.id)
    .then(function(data) {

        console.log("got: ", data);

        vm.view.loading = false;
        vm.relations = data;

        vm.hasRelations = vm.relations.documentTypes.length > 0 || vm.relations.mediaTypes.length > 0 || vm.relations.memberTypes.length > 0;


        console.log("vm: ", vm);

    });



}

angular.module("umbraco").controller("Umbraco.Editors.DataType.RelationsController", DataTypeRelationsController);