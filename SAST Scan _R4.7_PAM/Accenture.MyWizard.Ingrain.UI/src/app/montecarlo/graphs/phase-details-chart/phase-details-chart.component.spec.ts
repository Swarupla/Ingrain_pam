import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PhaseDetailsChartComponent } from './phase-details-chart.component';

describe('PhaseDetailsChartComponent', () => {
  let component: PhaseDetailsChartComponent;
  let fixture: ComponentFixture<PhaseDetailsChartComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PhaseDetailsChartComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PhaseDetailsChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
