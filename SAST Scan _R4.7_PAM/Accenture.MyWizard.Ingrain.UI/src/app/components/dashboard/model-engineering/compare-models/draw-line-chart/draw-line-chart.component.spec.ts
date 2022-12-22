import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DrawLineChartComponent } from './draw-line-chart.component';

describe('DrawLineChartComponent', () => {
  let component: DrawLineChartComponent;
  let fixture: ComponentFixture<DrawLineChartComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DrawLineChartComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DrawLineChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
