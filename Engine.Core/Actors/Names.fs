namespace R4nd0mApps.TddStud10.Engine.Actors

module ActorNames = 
    module System = 
        let Name = "TddStud10"
        let makeName = sprintf "akka://%s/user/%s" Name
    
    module DataStore = 
        let Name = "DataStore"
        let Path = System.makeName Name
    
    module IdEvents = 
        let Name = "IdeEvents"
        let Path = System.makeName Name

    module Runner = 
        let Name = "Runner"
        let Path = System.makeName Name
    
    module RunScheduler = 
        let Name = "RunScheduler"
        let Path = System.makeName Name

    module ProjectBuildCoordinator = 
        let Name = "ProjectBuildCoordinator"
        let Path = System.makeName Name
    
    module ProjectBuildStepsFactory =
        let Name = "ProjectBuildStepsFactory"
        let Path = System.makeName Name
    
    module ProjectSnapshotCreator = 
        let Name = "ProjectSnapshotCreator"
        let Path = System.makeName Name
    
    module ProjectFileFixer = 
        let Name = "ProjectFileFixer"
        let Path = System.makeName Name
    
    module ProjectBuilder = 
        let Name = "ProjectBuilder"
        let Path = System.makeName Name
    
    module AssemblyInstrumenter = 
        let Name = "AssemblyInstrumenter"
        let Path = System.makeName Name
    
    module AssemblyTestsDiscovererCoordinator = 
        let Name = "AssemblyTestsDiscovererCoordinator"
        let Path = System.makeName Name
    
    module AssemblyTestsDiscoverer = 
        let Name = "AssemblyTestsDiscoverer"
        let Path = System.makeName Name
    
    module AssemblySequencePointsDiscovererCoordinator = 
        let Name = "AssemblySequencePointsDiscovererCoordinator"
        let Path = System.makeName Name
    
    module AssemblySequencePointsDiscoverer = 
        let Name = "AssemblySequencePointsDiscoverer"
        let Path = System.makeName Name
    
    module TestRunnerCoordinator = 
        let Name = "TestRunnerCoordinator"
        let Path = System.makeName Name
    
    module TestRunner = 
        let Name = "TestRunner"
        let Path = System.makeName Name