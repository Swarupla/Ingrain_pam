import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TeachAndTestComponent } from './teach-and-test.component';

describe('TeachAndTestComponent', () => {
  let component: TeachAndTestComponent;
  let fixture: ComponentFixture<TeachAndTestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TeachAndTestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TeachAndTestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
