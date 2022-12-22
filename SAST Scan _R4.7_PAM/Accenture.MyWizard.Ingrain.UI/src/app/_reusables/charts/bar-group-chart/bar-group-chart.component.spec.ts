import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BarGroupChartComponent } from './bar-group-chart.component';

describe('BarGroupChartComponent', () => {
  let component: BarGroupChartComponent;
  let fixture: ComponentFixture<BarGroupChartComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BarGroupChartComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BarGroupChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
