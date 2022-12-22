import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CascadeModelsChartComponent } from './cascade-models-chart.component';

describe('CascadeModelsChartComponent', () => {
  let component: CascadeModelsChartComponent;
  let fixture: ComponentFixture<CascadeModelsChartComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CascadeModelsChartComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CascadeModelsChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
