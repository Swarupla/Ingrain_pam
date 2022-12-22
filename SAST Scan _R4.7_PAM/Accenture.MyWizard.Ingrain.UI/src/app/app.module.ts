import { BrowserModule } from '@angular/platform-browser';
import { NgModule, ErrorHandler, APP_INITIALIZER } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppComponent } from '../app/components/root/app.component';
import { AppRoutingModule } from '../app/_routes/app-routing.module';
import { HttpClientModule, HTTP_INTERCEPTORS } from '../../node_modules/@angular/common/http';
import { NotFoundComponent } from './components/not-found/not-found.component';
import { ProcessedDataComponent } from './components/processed-data/processed-data.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { HeaderComponent } from './components/header/header.component';
import { DashboardPanelComponent } from './components/dashboard/dashboard-panel/dashboard-panel.component';
import { ProblemStatementComponent } from './components/dashboard/problem-statement/problem-statement.component';
import { DataEngineeringComponent } from './components/dashboard/data-engineering/data-engineering.component';
import { ModelEngineeringComponent } from './components/dashboard/model-engineering/model-engineering.component';
import { DeployModelComponent } from './components/dashboard/deploy-model/deploy-model.component';
import { ToastrModule } from 'ngx-toastr';
import { FilterPipe } from './_pipes/filter.pipe';
import { HomeComponent } from './components/home/home.component';
import { PstTemplatesComponent } from './components/dashboard/problem-statement/pst-templates/pst-templates.component';
import {
  PstUseCaseDefinitionComponent
} from './components/dashboard/problem-statement/pst-use-case-definition/pst-use-case-definition.component';
import { HttpConfigInterceptor } from './_interceptor/httpconfig.interceptor';
import { ErrorsHandler } from './_errorHandler/error.handler';
import { DialogModule } from './dialog/dialog.module';
import { UploadFileComponent } from './components/upload-file/upload-file.component';
import { TruncatePublicModelnamePipe } from './_pipes/truncate-public-modelname.pipe';
import { TemplateNameModalComponent } from './components/template-name-modal/template-name-modal.component';
import { FileUploadProgressBarComponent } from './components/file-upload-progress-bar/file-upload-progress-bar.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { DataCleanupComponent } from './components/dashboard/data-engineering/data-cleanup/data-cleanup.component';
import { PreprocessDataComponent } from './components/dashboard/data-engineering/preprocess-data/preprocess-data.component';
import {
  DataVisualisationComponent
} from './components/dashboard/data-engineering/data-visualisation/data-visualisation.component';
import {
  PstFeatureMappingComponent
} from './components/dashboard/problem-statement/pst-feature-mapping-popup/pst-feature-mapping.component';
import { LoaderDisplayComponent } from './components/loader-display/loader-display.component';
import { ObjectToArrayPipe } from './_pipes/object-to-array.pipe';
import { CookieService } from 'ngx-cookie-service';
import {
  DatasourceChangeComponent
} from './components/dashboard/problem-statement/datasource-change-popup/datasource-change.component';
import { RegressionPopupComponent } from './components/dashboard/problem-statement/regression-popup/regression-popup.component';
import {
  RegressiondataComponent
} from './components/dashboard/problem-statement/regression-popup/regressiondata/regressiondata.component';
import { NgxPaginationModule } from 'ngx-pagination';
import { MultiSelectDropDownComponent } from './_reusables/multi-select-drop-down/multi-select-drop-down.component';
import { SearchPipe } from './_pipes/search.pipe';
import {
  FeatureSelectionComponent
} from './components/dashboard/model-engineering/feature-selection-model/feature-selection.component';
import { MyclientListComponent } from './components/header/myclient-list/myclient-list.component';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RecommendedAIComponent } from './components/dashboard/model-engineering/recommended-ai/recommended-ai.component';
import { TeachAndTestComponent } from './components/dashboard/model-engineering/teach-and-test/teach-and-test.component';
import {
  ModelDeletionPopupComponent
} from './components/dashboard/problem-statement/pst-templates/model-deletion-popup/model-deletion-popup.component';
import { CompareModelsComponent } from './components/dashboard/model-engineering/compare-models/compare-models.component';
import {
  SaveScenarioPopupComponent
} from './components/dashboard/model-engineering/teach-and-test/save-scenario-popup/save-scenario-popup.component';
import { D3DonutChartComponent } from './_reusables/d3-donut-chart/d3-donut-chart.component';
import {
  AddFilterComponent
} from './components/dashboard/data-engineering/preprocess-data/add-filter/add-filter.component';
import { BarChartComponent } from './_reusables/charts/bar-chart/bar-chart.component';
import { HorizontalChartComponent } from './_reusables/charts/horizontal-chart/horizontal-chart.component';
import { PublishModelComponent } from './components/dashboard/deploy-model/publish-model/publish-model.component';
import { DeployedModelComponent } from './components/dashboard/deploy-model/deployed-end-model/deployed-model.component';
import { FocusAreaComponent } from './components/dashboard/focus-area/focus-area.component';
import { UploadApiComponent } from './components/upload-api/upload-api.component';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatNativeDateModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AreaChartComponent } from './_reusables/charts/area-chart/area-chart.component';
import {
  HyperTuningComponent
} from './components/dashboard/model-engineering/teach-and-test/hyper-tuning/hyper-tuning.component';
import {
  WhatIfAnalysisComponent
} from './components/dashboard/model-engineering/teach-and-test/what-if-analysis/what-if-analysis.component';
// tslint:disable-next-line:max-line-length
import { HtVersionNameComponent } from './components/dashboard/model-engineering/teach-and-test/hyper-tuning/ht-version-name/ht-version-name.component';
import { DatePipe } from '@angular/common';
import {
  CustomColumnComponent
} from './components/dashboard/problem-statement/pst-feature-mapping-popup/add-custom-column-popup/custom-column.component';
import { ShowDataComponent } from './components/dashboard/data-engineering/preprocess-data/show-data/show-data.component';
import {
  ConfirmationPopUpComponent
} from './components/dashboard/data-engineering/preprocess-data/confirmation-pop-up/confirmation-popup.component';
import { LineChartComponent } from './_reusables/charts/line-chart/line-chart.component';
import { ViewDataComponent } from './components/dashboard/data-engineering/preprocess-data/view-data/view-data.component';
import {
  DrawLineChartComponent
} from './components/dashboard/model-engineering/compare-models/draw-line-chart/draw-line-chart.component';
import { ModalModule } from 'ngx-bootstrap/modal';
import { SessionTimeoutPopupComponent } from './components/header/session-timeout-popup/session-timeout-popup.component';
import { PopoverModule } from 'ngx-bootstrap/popover';
import { HalfDonutComponent } from './_reusables/half-donut/half-donut.component';
import { NgIdleKeepaliveModule } from '@ng-idle/keepalive';
// this includes the core NgIdleModule but includes keepalive providers for easy wireup

import { MomentModule } from 'angular2-moment';
import { FilesMappingModalComponent } from './components/files-mapping-modal/files-mapping-modal.component';
import { AddFeatureBoardComponent } from './components/dashboard/data-engineering/data-cleanup/add-feature-board/add-feature-board.component';

import { TxtPreprocessComponent } from './components/dashboard/data-engineering/preprocess-data/txt-preprocess/txt-preprocess.component';
import { OperationTypeDropdownComponent } from './_reusables/add-feature/operation-type-dropdown/operation-type-dropdown.component';
import { ColumnDropdownComponent } from './_reusables/add-feature/column-dropdown/column-dropdown.component';
import { MatSelect } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PredictionCardComponent } from './_reusables/what-if-analysis/prediction-card/prediction-card.component';
import { PredictionDonutComponent } from './_reusables/what-if-analysis/prediction-card/prediction-donut/prediction-donut.component';
import { FrequencyLineChartComponent } from './_reusables/what-if-analysis/frequency-line-chart/frequency-line-chart.component';
import { WordCloudImageComponent } from './_reusables/what-if-analysis/word-cloud-image/word-cloud-image.component';
import { MissingValuesComponent } from './components/dashboard/data-engineering/preprocess-data/missing-values/missing-values.component';
import { FiltersDataComponent } from './components/dashboard/data-engineering/preprocess-data/filters-data/filters-data.component';

import { Ng5SliderModule } from 'ng5-slider';
import { NgxMatSelectSearchModule } from 'ngx-mat-select-search';
// tslint:disable-next-line: max-line-length
import { DataModificationComponent } from './components/dashboard/data-engineering/preprocess-data/data-modification/data-modification.component';
import { DataEncodingComponent } from './components/dashboard/data-engineering/preprocess-data/data-encoding/data-encoding.component';
import { UserProfileComponent } from './components/header/user-profile/user-profile.component';
import { FooterComponent } from './components/footer/footer.component';
import { AddNewappComponent } from './components/dashboard/deploy-model/add-newapp/add-newapp.component';
import { DropdownsearchPipe } from './_pipes/dropdownsearch.pipe';
import { UserHelpComponent } from './components/header/user-help/user-help.component';
import { LetsgetstartedPopupComponent } from './components/letsgetstarted-popup/letsgetstarted-popup.component';
import { StatusPopupComponent } from './components/status-popup/status-popup.component';
// tslint:disable-next-line: max-line-length
import { ModelsTrainingStatusPopupComponent } from './components/header/models-training-status-popup/models-training-status-popup.component';
import { StatusPipe } from './_pipes/status.pipe';
import { NlpServicesComponent } from './components/dashboard/nlp-services/nlp-services.component';

import { MyVisualizationComponent } from './components/dashboard/my-visualization/my-visualization.component';
import { StackChartComponent } from './_reusables/charts/stack-chart/stack-chart.component';
import { NgxJsonViewerModule } from 'ngx-json-viewer';

import { VirtualAssistantConfig, VirtualAssistantModule } from 'virtual-agent-10-r4.0';
import { InfoTooltipComponent } from './_reusables/info-tooltip/info-tooltip.component';
import { InfoConfirmPopupComponent } from './components/info-confirm-popup/info-confirm-popup.component';
import { CookieDeclinePopupComponent } from './components/footer/cookie-decline-popup/cookie-decline-popup.component';

// MonteCarlo Components
import { GridTableComponent } from './montecarlo/ADSP/grid-table/grid-table.component';
import { FileUploadMappingComponent } from './montecarlo/ADSP/file-upload-mapping/file-upload-mapping.component';
import { AdspLandingComponent } from './montecarlo/ADSP/adsp-landing/adsp-landing.component';
import { InputSimulationComponent } from './montecarlo/ADSP/input-simulation/input-simulation.component';
import { OutputSimulationComponent } from './montecarlo/ADSP/output-simulation/output-simulation.component';
import { UploadComponent } from './montecarlo/shared/component/upload/upload.component';
import { GenericUploadComponent } from './montecarlo/Generic/generic-upload/generic-upload.component';
import { SimulationHeaderComponent } from './montecarlo/shared/component/simulation-header/simulation-header.component';
import { HistogramChartComponent } from './montecarlo/graphs/histogram-chart/histogram-chart.component';
import { SensitivityAnalysisChartComponent } from './montecarlo/graphs/sensitivity-analysis-chart/sensitivity-analysis-chart.component';
import { GenericGridComponent } from './montecarlo/Generic/generic-grid/generic-grid.component';
import { GenericInputComponent } from './montecarlo/Generic/generic-input/generic-input.component';
import { GenericOutputComponent } from './montecarlo/Generic/generic-output/generic-output.component';
import { GenericLandingComponent } from './montecarlo/Generic/generic-landing/generic-landing.component';

import { MatExpansionModule } from '@angular/material/expansion';
import { MatTableModule } from '@angular/material/table';
import { ValidRecordDetailsPopupComponent } from './components/dashboard/problem-statement/valid-record-details-popup/valid-record-details-popup.component';
import { UploadSimilarityAnalysisAPIComponent } from './components/upload-similarity-analysis-api/upload-similarity-analysis-api.component';
import { IngestDataColumnNamesComponent } from './components/ingest-data-column-names/ingest-data-column-names.component';
// optional, provides moment-style pipes for date formatting
import { MatDialogModule, MatDialogRef, MatDialog } from '@angular/material/dialog';
import { DialogService } from 'src/app/dialog/dialog.service';
import { LineChartMonitoringComponent } from './_reusables/charts/model monitoring/line-chart-monitoring/line-chart-monitoring.component';
import { ModelMonitoringComponent } from './components/dashboard/deploy-model/model-monitoring/model-monitoring.component';
import { PredictionVisualization } from './components/dashboard/deploy-model/prediction-visualization/prediction-visualization.component';
import { PhaseDetailsChartComponent } from './montecarlo/graphs/phase-details-chart/phase-details-chart.component';
import { CurvedVerticalBarGraphComponent } from './montecarlo/graphs/curved-vertical-bar-graph/curved-vertical-bar-graph.component';
import { CurvedHorizontalBarGraphComponent } from './montecarlo/graphs/curved-horizontal-bar-graph/curved-horizontal-bar-graph.component';
import { AuthorizationInterceptor } from './_interceptor/authorization.interceptor';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';

import { ChooseCascadeModelsComponent } from './components/dashboard/problem-statement/choose-cascade-models/choose-cascade-models.component';
import { CascadeModelsComponent } from './components/dashboard/problem-statement/cascade-models/cascade-models.component';
import { MapCascadeModelsComponent } from './components/dashboard/problem-statement/map-cascade-models/map-cascade-models.component';
import { TeachTestCascadeModelsComponent } from './components/dashboard/problem-statement/teach-test-cascade-models/teach-test-cascade-models.component';
import { DeployCascadeModelsComponent } from './components/dashboard/problem-statement/deploy-cascade-models/deploy-cascade-models.component';
import { CascadeModelsChartComponent } from './_reusables/cascade-models-chart/cascade-models-chart.component';
import { PublishCascadeModelsComponent } from './components/dashboard/problem-statement/publish-cascade-models/publish-cascade-models.component';
import { NestedMultiSelectDropDownComponent } from './_reusables/nested-multi-select-drop-down/nested-multi-select-drop-down.component';
import { MatIconModule } from '@angular/material/icon';
import { BubbleChartComponent } from './_reusables/charts/bubble-chart/bubble-chart.component';
import { MyUtilitiesComponent } from './components/dashboard/problem-statement/my-utilities/my-utilities.component';
import { UtilityUploadComponent } from './components/utility-upload/utility-upload.component';
import { DatasetDeletionPopupComponent } from './components/dashboard/problem-statement/dataset-deletion-popup/dataset-deletion-popup.component';
import { ViewDatasetrecordsComponent } from './components/dashboard/problem-statement/view-datasetrecords/view-datasetrecords.component';
import { UtilityApiuploadComponent } from './components/utility-apiupload/utility-apiupload.component';
import { AssetsTrackingComponent } from './components/assets-usage/assets-tracking/assets-tracking.component';
import { ShowVideoComponent } from './components/header/show-video/show-video.component';
import { UploadDataComponent } from './cascadeVisualization/upload-data/upload-data.component';
import { CascadeVisualizationGraphComponent } from './cascadeVisualization/graphs/cascade-visualization-graph/cascade-visualization-graph.component';
import { ShowCascadeDataComponent } from './cascadeVisualization/show-data/show-data.component';
import { FlipCardComponent } from './_reusables/flip-card/flip-card.component';
import { D3TreemapComponent } from './_reusables/d3-treemap/d3-treemap.component';
import { BarGroupChartComponent } from './_reusables/charts/bar-group-chart/bar-group-chart.component';
import { UserNotificationpopupComponent } from './components/user-notificationpopup/user-notificationpopup.component';
import { FmVisualizationGraphComponent } from './FMVisualization/graphs/fm-visualization-graph/fm-visualization-graph.component';
import { InferenceEngineComponent } from './inferenceEngine/inference-engine/inference-engine.component';
import { LoaderScreenIeComponent } from './inferenceEngine/loader-screen-ie/loader-screen-ie.component';
import { SampleInferenceComponent } from './inferenceEngine/inference-models/sample-inference/sample-inference.component';
import { ConfigInferenceComponent } from './inferenceEngine/inference-models/config-inference/config-inference.component';
import { InferenceModelsComponent } from './inferenceEngine/inference-models/inference-models.component';
import { IeConfigurationComponent } from './inferenceEngine/ie-configuration/ie-configuration.component';
import { PublishedUseCaseComponent } from './inferenceEngine/published-use-case/published-use-case.component';
import { MarketplaceFileEncryptionComponent } from './components/marketplace-file-encryption/marketplace-file-encryption.component';
import { ConfigureCertificationFlagComponent } from './components/configure-certification-flag/configure-certification-flag.component';
import { CompletionCertificateComponent } from './components/dashboard/deploy-model/completion-certificate/completion-certificate.component';
import { ViewResponseComponent } from './components/dashboard/problem-statement/pst-use-case-definition/view-response/view-response.component';
import { CustomDataService } from './_services/custom-data.service';
import { ModalNameDialogComponent } from './_reusables/modal-name-dialog/modal-name-dialog.component';
import { CustomDataApiComponent } from './_reusables/custom-data/custom-data-api/custom-data-api.component';
import { CustomQuerySaveComponent } from './_reusables/custom-data/custom-query-save/custom-query-save.component';
import { CustomDataViewComponent } from './components/dashboard/nlp-services/custom-data-view/custom-data-view.component';
import { CustomConfigComponent } from './components/dashboard/deploy-model/custom-config/custom-config.component';
import { DateColumnComponent } from './components/dashboard/problem-statement/pst-use-case-definition/date-column/date-column.component';
import { TargetNodeComponent } from './_reusables/custom-data/target-node/target-node.component';
import { ArchivedModelListComponent } from './components/dashboard/problem-statement/pst-templates/archived-model-list/archived-model-list.component';
import { AuthenticationService } from './msal-authentication/msal-authentication.service';
import { MsalModule } from '@azure/msal-angular';
import { AdDashboardComponent } from './components/anomaly-detection/ad-dashboard/ad-dashboard.component';
import { AdUploadDataComponent } from './components/anomaly-detection/ad-upload-data/ad-upload-data.component';
import { AdUsecaseDefinitionComponent } from './components/anomaly-detection/ad-usecase-definition/ad-usecase-definition.component';
import { AdModelEngineeringComponent } from './components/anomaly-detection/ad-model-engineering/ad-model-engineering.component';
import { AdRecommendedAiComponent } from './components/anomaly-detection/ad-model-engineering/ad-recommended-ai/ad-recommended-ai.component';
import { AdDeployModelComponent } from './components/anomaly-detection/ad-deploy-model/ad-deploy-model.component';
import { AdDeployedModelComponent } from './components/anomaly-detection/ad-deploy-model/ad-deployed-model/ad-deployed-model.component';
import { AdPublishModelComponent } from './components/anomaly-detection/ad-deploy-model/ad-publish-model/ad-publish-model.component';
import { UploadProgressComponent } from './components/anomaly-detection/upload-progress/upload-progress.component';
import { AnomalyDetectionComponent } from './components/anomaly-detection/anomaly-detection.component';
import { AdBarChartComponent } from './components/anomaly-detection/ad-model-engineering/charts/ad-bar-chart/ad-bar-chart.component';
import { ChangeDataSourceComponent } from './components/anomaly-detection/ad-usecase-definition/change-data-source/change-data-source.component';
import { AdDeployedModelListComponent } from './components/anomaly-detection/ad-deploy-model/ad-deployed-model-list/ad-deployed-model-list.component';
import { AdLineChartComponent } from './components/anomaly-detection/ad-model-engineering/charts/ad-line-chart/ad-line-chart.component';

const appInitializerFn = (appConfig: EnvironmentService) => {
  return () => {
    return appConfig.loadConfig();
  };
};


@NgModule({
  declarations: [
    AppComponent,
    NotFoundComponent,
    ProcessedDataComponent,
    DashboardComponent,
    HeaderComponent,
    DashboardPanelComponent,
    ProblemStatementComponent,
    DataEngineeringComponent,
    ModelEngineeringComponent,
    DeployModelComponent,
    FilterPipe,
    HomeComponent,
    ShowDataComponent,
    PstTemplatesComponent,
    PstUseCaseDefinitionComponent,
    TruncatePublicModelnamePipe,
    UploadFileComponent,
    TemplateNameModalComponent,
    FileUploadProgressBarComponent,
    DataCleanupComponent,
    PreprocessDataComponent,
    DataVisualisationComponent,
    PstFeatureMappingComponent,
    LoaderDisplayComponent,
    ObjectToArrayPipe,
    DatasourceChangeComponent,
    RegressionPopupComponent,
    RegressiondataComponent,
    MultiSelectDropDownComponent,
    SearchPipe,
    HalfDonutComponent,
    FeatureSelectionComponent,
    MyclientListComponent,
    RecommendedAIComponent,
    CompareModelsComponent,
    TeachAndTestComponent,
    ModelDeletionPopupComponent,
    SaveScenarioPopupComponent,
    D3DonutChartComponent,
    DatasourceChangeComponent,
    RegressionPopupComponent,
    RegressiondataComponent,
    MultiSelectDropDownComponent,
    SearchPipe,
    FeatureSelectionComponent,
    MyclientListComponent,
    RecommendedAIComponent,
    TeachAndTestComponent,
    ModelDeletionPopupComponent,
    AddFilterComponent,
    SaveScenarioPopupComponent,
    BarChartComponent,
    HorizontalChartComponent,
    PublishModelComponent,
    DeployedModelComponent,
    FocusAreaComponent,
    UploadApiComponent,
    AreaChartComponent,
    HyperTuningComponent,
    WhatIfAnalysisComponent,
    HtVersionNameComponent,
    CustomColumnComponent,
    ConfirmationPopUpComponent,
    LineChartComponent,
    ViewDataComponent,
    DrawLineChartComponent,
    SessionTimeoutPopupComponent,
    AddFeatureBoardComponent,
    SessionTimeoutPopupComponent,
    FilesMappingModalComponent,
    OperationTypeDropdownComponent,
    ColumnDropdownComponent,
    TxtPreprocessComponent,
    PredictionCardComponent,
    PredictionDonutComponent,
    FrequencyLineChartComponent,
    WordCloudImageComponent,
    MissingValuesComponent,
    FiltersDataComponent,
    DataModificationComponent,
    DataEncodingComponent,
    UserProfileComponent,
    FooterComponent,
    UserHelpComponent,
    LetsgetstartedPopupComponent,
    StatusPopupComponent,
    ModelsTrainingStatusPopupComponent,
    StatusPipe,
    AddNewappComponent,
    DropdownsearchPipe,
    MyVisualizationComponent,
    StackChartComponent,
    NlpServicesComponent,
    InfoTooltipComponent,
    InfoConfirmPopupComponent,
    CookieDeclinePopupComponent,
    // MonteCarlo Components
    GridTableComponent,
    FileUploadMappingComponent,
    AdspLandingComponent,
    InputSimulationComponent,
    OutputSimulationComponent,
    HistogramChartComponent,
    OutputSimulationComponent,
    UploadComponent,
    GenericUploadComponent,
    SimulationHeaderComponent,
    GenericGridComponent,
    GenericInputComponent,
    GenericOutputComponent,
    GenericLandingComponent,
    SensitivityAnalysisChartComponent,
    ValidRecordDetailsPopupComponent,
    UploadSimilarityAnalysisAPIComponent,
    IngestDataColumnNamesComponent,
    ModelMonitoringComponent,
    PredictionVisualization,
    LineChartMonitoringComponent,
    CurvedVerticalBarGraphComponent,
    CurvedHorizontalBarGraphComponent,
    PhaseDetailsChartComponent,
    ChooseCascadeModelsComponent,
    CascadeModelsComponent,
    MapCascadeModelsComponent,
    TeachTestCascadeModelsComponent,
    DeployCascadeModelsComponent,
    CascadeModelsChartComponent,
    PublishCascadeModelsComponent,
    NestedMultiSelectDropDownComponent,
    BubbleChartComponent,
    MyUtilitiesComponent,
    UtilityUploadComponent,
    DatasetDeletionPopupComponent,
    ViewDatasetrecordsComponent,
    UtilityApiuploadComponent,
    AssetsTrackingComponent,
    ShowVideoComponent,
    UploadDataComponent,
    CascadeVisualizationGraphComponent,
    ShowCascadeDataComponent,
    FlipCardComponent,
    D3TreemapComponent,
    BarGroupChartComponent,
    UserNotificationpopupComponent,
    FmVisualizationGraphComponent,
    InferenceEngineComponent,
    LoaderScreenIeComponent,
    SampleInferenceComponent,
    ConfigInferenceComponent,
    IeConfigurationComponent,
    InferenceModelsComponent,
    PublishedUseCaseComponent,
    MarketplaceFileEncryptionComponent,
    ConfigureCertificationFlagComponent,
    CompletionCertificateComponent,
    ViewResponseComponent,
    ModalNameDialogComponent,
    CustomDataApiComponent,
    CustomQuerySaveComponent,
    CustomDataViewComponent,
    CustomConfigComponent,
    DateColumnComponent,
    TargetNodeComponent,
    ArchivedModelListComponent,
    AdDashboardComponent,
    AdUploadDataComponent,
    AdUsecaseDefinitionComponent,
    AdModelEngineeringComponent,
    AdRecommendedAiComponent,
    AdDeployModelComponent,
    AdDeployedModelComponent,
    AdPublishModelComponent,
    UploadProgressComponent,
    AnomalyDetectionComponent,
    AdBarChartComponent,
    ChangeDataSourceComponent,
    AdDeployedModelListComponent,
    AdLineChartComponent
  ],
  imports: [
    BrowserModule,
    FormsModule,
    AppRoutingModule,
    HttpClientModule,
    Ng5SliderModule,
    ToastrModule.forRoot(
      {}
    ),
    DialogModule,
    BrowserAnimationsModule,
    NgxPaginationModule,
    // tslint:disable-next-line: deprecation
    NgbModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatSelectModule,
    MatIconModule,
    ReactiveFormsModule,
    MatCheckboxModule,
    MatSlideToggleModule,
    MatRadioModule,
    ModalModule.forRoot(),
    PopoverModule.forRoot(),
    MomentModule,
    MatTooltipModule,
    MatExpansionModule,
    MatTableModule,
    NgIdleKeepaliveModule.forRoot(),
    NgxMatSelectSearchModule,
    NgxJsonViewerModule,
    VirtualAssistantModule.forRoot(),
    // MonteCarlo Modules
    MatDialogModule,
    NgbModule,
    MsalModule
  ],
  providers: [
    AuthenticationService,
    {
      provide: APP_INITIALIZER,
      deps: [EnvironmentService, AuthenticationService],
      multi: true,
      useFactory: (environmentService: EnvironmentService,
        msalauthenticationService: AuthenticationService
      ) => (): Promise<void> => {
          return environmentService.loadConfig().then(() => {
            return msalauthenticationService.bootstrap();
          });
        }
    },

    {
      provide: ErrorHandler,
      useClass: ErrorsHandler,
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthorizationInterceptor,
      // useClass: HttpConfigInterceptor,
      multi: true
    },
    {
      provide: MatDialogRef,
      useValue: {}
    },
    DialogService,
    CookieService,
    MatDatepickerModule,
    MatExpansionModule,
    MatTableModule,
    DatePipe,
    EnvironmentService,
    CustomDataService
  ],
  entryComponents: [ViewDataComponent, ConfirmationPopUpComponent, ShowDataComponent, UploadFileComponent, TemplateNameModalComponent,
    FileUploadProgressBarComponent, PstFeatureMappingComponent, DatasourceChangeComponent, RegressionPopupComponent,
    ModelDeletionPopupComponent, SaveScenarioPopupComponent, AddFilterComponent, UploadApiComponent,
    HtVersionNameComponent, SessionTimeoutPopupComponent, FilesMappingModalComponent, LetsgetstartedPopupComponent,
    StatusPopupComponent, ModelsTrainingStatusPopupComponent, InfoConfirmPopupComponent, CookieDeclinePopupComponent,
    ValidRecordDetailsPopupComponent, UploadSimilarityAnalysisAPIComponent, IngestDataColumnNamesComponent, NestedMultiSelectDropDownComponent,
    UtilityUploadComponent, DatasetDeletionPopupComponent, ViewDatasetrecordsComponent, UtilityApiuploadComponent, ShowCascadeDataComponent, UserNotificationpopupComponent
  ],
  bootstrap: [AppComponent]
})

export class AppModule {
}
