import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FrequencyLineChartComponent } from './frequency-line-chart.component';

describe('FrequencyLineChartComponent', () => {
  let component: FrequencyLineChartComponent;
  let fixture: ComponentFixture<FrequencyLineChartComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ FrequencyLineChartComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FrequencyLineChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
