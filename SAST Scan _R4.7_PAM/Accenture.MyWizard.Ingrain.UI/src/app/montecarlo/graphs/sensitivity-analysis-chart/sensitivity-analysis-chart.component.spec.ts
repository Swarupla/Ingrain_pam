import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SensitivityAnalysisChartComponent } from './sensitivity-analysis-chart.component';

describe('SensitivityAnalysisChartComponent', () => {
  let component: SensitivityAnalysisChartComponent;
  let fixture: ComponentFixture<SensitivityAnalysisChartComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SensitivityAnalysisChartComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SensitivityAnalysisChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
