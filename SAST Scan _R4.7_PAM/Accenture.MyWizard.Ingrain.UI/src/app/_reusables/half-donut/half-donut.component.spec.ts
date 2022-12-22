import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { HalfDonutComponent } from './half-donut.component';

describe('HalfDonutComponent', () => {
  let component: HalfDonutComponent;
  let fixture: ComponentFixture<HalfDonutComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ HalfDonutComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(HalfDonutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
