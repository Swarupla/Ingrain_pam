import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdLineChartComponent } from './ad-line-chart.component';

describe('AdLineChartComponent', () => {
  let component: AdLineChartComponent;
  let fixture: ComponentFixture<AdLineChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdLineChartComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AdLineChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
